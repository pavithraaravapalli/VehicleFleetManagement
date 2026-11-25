using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VehicleFleetManagement.Data;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.Services.Interfaces;
using VehicleFleetManagement.ViewModels;

namespace VehicleFleetManagement.Services.Implementations
{
    /// <summary>
    /// Implements reporting and business analytics engine using Entity Framework Core and LINQ queries.
    /// Provides real-time calculations directly from SQL Server for executive dashboards and audit reports.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMaintenanceService _maintenanceService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ApplicationDbContext context,
            IMaintenanceService maintenanceService,
            ILogger<ReportService> logger)
        {
            _context = context;
            _maintenanceService = maintenanceService;
            _logger = logger;
        }

        // ==========================================
        // 1. Executive Admin Dashboard Metrics & Chart Datasets
        // ==========================================
        public async Task<AdminDashboardViewModel> GetAdminDashboardStatsAsync()
        {
            var vm = new AdminDashboardViewModel();

            // Vehicle Fleet Counts
            vm.TotalVehicles = await _context.Vehicles.CountAsync();
            vm.AvailableVehicles = await _context.Vehicles.CountAsync(v => v.Status == VehicleStatus.Available);
            vm.AssignedVehicles = await _context.Vehicles.CountAsync(v => v.Status == VehicleStatus.Assigned);
            vm.UnderMaintenanceVehicles = await _context.Vehicles.CountAsync(v => v.Status == VehicleStatus.UnderMaintenance);

            // Driver Counts
            vm.TotalDrivers = await _context.Drivers.CountAsync();
            vm.ActiveDrivers = await _context.Drivers.CountAsync(d => d.AvailabilityStatus == DriverStatus.Available || d.AvailabilityStatus == DriverStatus.OnTrip);

            // Trip Lifecycle Counts
            vm.TotalTrips = await _context.Trips.CountAsync();
            vm.ActiveTrips = await _context.Trips.CountAsync(t => t.TripStatus == TripStatusEnum.InProgress);
            vm.CompletedTrips = await _context.Trips.CountAsync(t => t.TripStatus == TripStatusEnum.Completed);
            vm.PendingTrips = await _context.Trips.CountAsync(t => t.TripStatus == TripStatusEnum.Scheduled);

            // Operational Financial Expenses
            vm.TotalFuelExpenses = await _context.FuelLogs.SumAsync(f => (decimal?)f.TotalCost) ?? 0m;
            vm.TotalMaintenanceExpenses = await _context.Maintenances.SumAsync(m => (decimal?)m.Cost) ?? 0m;

            // Proactive Maintenance Due Alerts (within 7 days)
            var upcoming = await _maintenanceService.GetUpcomingMaintenanceAlertsAsync(7);
            vm.UpcomingMaintenanceAlerts = upcoming;
            vm.UpcomingMaintenanceAlertsCount = upcoming.Count();

            // Recent Fleet Activities
            vm.RecentTrips = await _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Driver)
                .OrderByDescending(t => t.StartDate)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            vm.RecentVehicles = await _context.Vehicles
                .OrderByDescending(v => v.CreatedAt)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            // Chart Data 1: Vehicle Status Breakdown
            vm.VehicleStatusLabels = new List<string> { "Available", "Assigned / On Trip", "Under Maintenance" };
            vm.VehicleStatusCounts = new List<int> { vm.AvailableVehicles, vm.AssignedVehicles, vm.UnderMaintenanceVehicles };

            // Chart Data 2: Trip Status Breakdown
            var scheduledCount = await _context.Trips.CountAsync(t => t.TripStatus == TripStatusEnum.Scheduled);
            var inProgressCount = await _context.Trips.CountAsync(t => t.TripStatus == TripStatusEnum.InProgress);
            var completedCount = await _context.Trips.CountAsync(t => t.TripStatus == TripStatusEnum.Completed);
            var cancelledCount = await _context.Trips.CountAsync(t => t.TripStatus == TripStatusEnum.Cancelled);

            vm.TripStatusLabels = new List<string> { "Scheduled", "In Progress", "Completed", "Cancelled" };
            vm.TripStatusCounts = new List<int> { scheduledCount, inProgressCount, completedCount, cancelledCount };

            // Chart Data 3: Last 6 Months Fuel vs Maintenance Costs
            var now = DateTime.UtcNow;
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                int m = targetMonth.Month;
                int y = targetMonth.Year;
                string monthLabel = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m) + " " + y;
                vm.ExpenseMonths.Add(monthLabel);

                var monthlyFuel = await _context.FuelLogs
                    .Where(f => f.FuelDate.Year == y && f.FuelDate.Month == m)
                    .SumAsync(f => (decimal?)f.TotalCost) ?? 0m;
                vm.MonthlyFuelData.Add(monthlyFuel);

                var monthlyMaint = await _context.Maintenances
                    .Where(mn => mn.ServiceDate.Year == y && mn.ServiceDate.Month == m)
                    .SumAsync(mn => (decimal?)mn.Cost) ?? 0m;
                vm.MonthlyMaintenanceData.Add(monthlyMaint);
            }

            return vm;
        }

        // ==========================================
        // 2. Vehicle Utilization Report
        // ==========================================
        public async Task<IEnumerable<VehicleUtilizationReportViewModel>> GetVehicleUtilizationReportAsync(DateTime? fromDate, DateTime? toDate)
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Trips)
                .AsNoTracking()
                .ToListAsync();

            var reportList = new List<VehicleUtilizationReportViewModel>();

            int daysInRange = 30; // default baseline window
            if (fromDate.HasValue && toDate.HasValue && toDate.Value >= fromDate.Value)
            {
                daysInRange = Math.Max(1, (int)(toDate.Value - fromDate.Value).TotalDays + 1);
            }

            foreach (var v in vehicles)
            {
                var tripsQuery = v.Trips.AsEnumerable();
                if (fromDate.HasValue) tripsQuery = tripsQuery.Where(t => t.StartDate >= fromDate.Value);
                if (toDate.HasValue) tripsQuery = tripsQuery.Where(t => t.StartDate <= toDate.Value.AddDays(1).AddTicks(-1));

                var trips = tripsQuery.ToList();
                var totalTrips = trips.Count;
                var completedTrips = trips.Count(t => t.TripStatus == TripStatusEnum.Completed);
                var totalDistance = trips.Sum(t => t.Distance);
                var activeDays = trips.Select(t => t.StartDate.Date).Distinct().Count();

                decimal utilRate = daysInRange > 0
                    ? Math.Min(100m, Math.Round(((decimal)activeDays / daysInRange) * 100m, 1))
                    : 0m;

                reportList.Add(new VehicleUtilizationReportViewModel
                {
                    VehicleId = v.VehicleId,
                    RegistrationNumber = v.RegistrationNumber,
                    VehicleName = v.VehicleName,
                    VehicleType = v.VehicleType,
                    CurrentStatus = v.Status,
                    TotalTrips = totalTrips,
                    CompletedTrips = completedTrips,
                    TotalDistanceKm = totalDistance,
                    CurrentOdometer = v.CurrentOdometer,
                    ActiveDaysInPeriod = activeDays,
                    UtilizationPercentage = utilRate
                });
            }

            return reportList.OrderByDescending(r => r.TotalDistanceKm);
        }

        // ==========================================
        // 3. Driver Trip History Report
        // ==========================================
        public async Task<IEnumerable<DriverTripHistoryReportViewModel>> GetDriverTripHistoryReportAsync(int? driverId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Drivers
                .Include(d => d.Trips)
                .ThenInclude(t => t.Vehicle)
                .AsNoTracking()
                .AsQueryable();

            if (driverId.HasValue && driverId.Value > 0)
            {
                query = query.Where(d => d.DriverId == driverId.Value);
            }

            var drivers = await query.ToListAsync();
            var result = new List<DriverTripHistoryReportViewModel>();

            foreach (var d in drivers)
            {
                var tripsQuery = d.Trips.AsEnumerable();
                if (fromDate.HasValue) tripsQuery = tripsQuery.Where(t => t.StartDate >= fromDate.Value);
                if (toDate.HasValue) tripsQuery = tripsQuery.Where(t => t.StartDate <= toDate.Value.AddDays(1).AddTicks(-1));

                var trips = tripsQuery.OrderByDescending(t => t.StartDate).ToList();

                result.Add(new DriverTripHistoryReportViewModel
                {
                    DriverId = d.DriverId,
                    FullName = d.FullName,
                    LicenseNumber = d.LicenseNumber,
                    PhoneNumber = d.PhoneNumber,
                    AvailabilityStatus = d.AvailabilityStatus,
                    TotalAssignedTrips = trips.Count,
                    CompletedTrips = trips.Count(t => t.TripStatus == TripStatusEnum.Completed),
                    InProgressTrips = trips.Count(t => t.TripStatus == TripStatusEnum.InProgress),
                    CancelledTrips = trips.Count(t => t.TripStatus == TripStatusEnum.Cancelled),
                    TotalDistanceDrivenKm = trips.Sum(t => t.Distance),
                    RecentTrips = trips.Take(10).ToList()
                });
            }

            return result.OrderByDescending(r => r.TotalDistanceDrivenKm);
        }

        // ==========================================
        // 4. Trip Status Report (Completed / Pending)
        // ==========================================
        public async Task<TripsReportViewModel> GetTripsReportAsync(string? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Driver)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.TripStatus == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.StartDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var inclusiveEnd = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.StartDate <= inclusiveEnd);
            }

            var trips = await query.OrderByDescending(t => t.StartDate).ToListAsync();
            var totalDistance = trips.Sum(t => t.Distance);

            string title = "Fleet Journey & Trip Operations Report";
            if (status == TripStatusEnum.Completed) title = "Completed Trips Audit Report";
            else if (status == TripStatusEnum.Scheduled) title = "Pending & Scheduled Trips Dispatch Report";

            return new TripsReportViewModel
            {
                ReportTitle = title,
                StatusFilter = status ?? "All Statuses",
                FromDate = fromDate,
                ToDate = toDate,
                TotalCount = trips.Count,
                TotalDistanceKm = totalDistance,
                Trips = trips
            };
        }

        // ==========================================
        // 5. Monthly Fuel Expenses Report
        // ==========================================
        public async Task<MonthlyFuelExpensesReportViewModel> GetMonthlyFuelExpensesReportAsync(int year)
        {
            var logs = await _context.FuelLogs
                .Where(f => f.FuelDate.Year == year)
                .AsNoTracking()
                .ToListAsync();

            var monthlySummaries = new List<MonthlyFuelExpenseSummary>();

            for (int m = 1; m <= 12; m++)
            {
                var monthLogs = logs.Where(f => f.FuelDate.Month == m).ToList();
                var cost = monthLogs.Sum(f => f.TotalCost);
                var qty = monthLogs.Sum(f => f.FuelQuantity);

                monthlySummaries.Add(new MonthlyFuelExpenseSummary
                {
                    Year = year,
                    Month = m,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m),
                    TotalCost = cost,
                    TotalQuantity = qty,
                    RefillCount = monthLogs.Count
                });
            }

            var annualCost = monthlySummaries.Sum(s => s.TotalCost);
            var annualLitres = monthlySummaries.Sum(s => s.TotalQuantity);
            var annualRefills = monthlySummaries.Sum(s => s.RefillCount);

            return new MonthlyFuelExpensesReportViewModel
            {
                SelectedYear = year,
                AnnualTotalCost = annualCost,
                AnnualTotalLitres = annualLitres,
                AnnualRefillCount = annualRefills,
                MonthlySummaries = monthlySummaries
            };
        }

        // ==========================================
        // 6. Maintenance Expense Report
        // ==========================================
        public async Task<IEnumerable<MaintenanceExpenseReportViewModel>> GetMaintenanceExpenseReportAsync(DateTime? fromDate, DateTime? toDate, int? vehicleId)
        {
            var query = _context.Vehicles
                .Include(v => v.Maintenances)
                .AsNoTracking()
                .AsQueryable();

            if (vehicleId.HasValue && vehicleId.Value > 0)
            {
                query = query.Where(v => v.VehicleId == vehicleId.Value);
            }

            var vehicles = await query.ToListAsync();
            var report = new List<MaintenanceExpenseReportViewModel>();

            foreach (var v in vehicles)
            {
                var maintQuery = v.Maintenances.AsEnumerable();
                if (fromDate.HasValue) maintQuery = maintQuery.Where(m => m.ServiceDate >= fromDate.Value);
                if (toDate.HasValue) maintQuery = maintQuery.Where(m => m.ServiceDate <= toDate.Value.AddDays(1).AddTicks(-1));

                var records = maintQuery.OrderByDescending(m => m.ServiceDate).ToList();

                report.Add(new MaintenanceExpenseReportViewModel
                {
                    VehicleId = v.VehicleId,
                    RegistrationNumber = v.RegistrationNumber,
                    VehicleName = v.VehicleName,
                    ServiceCount = records.Count,
                    TotalExpense = records.Sum(r => r.Cost),
                    MaintenanceRecords = records
                });
            }

            return report.OrderByDescending(r => r.TotalExpense);
        }

        // ==========================================
        // 7. Fuel Consumption & Mileage Report
        // ==========================================
        public async Task<IEnumerable<FuelConsumptionReportViewModel>> GetFuelConsumptionReportAsync(DateTime? fromDate, DateTime? toDate, int? vehicleId)
        {
            var query = _context.Vehicles
                .Include(v => v.FuelLogs)
                .AsNoTracking()
                .AsQueryable();

            if (vehicleId.HasValue && vehicleId.Value > 0)
            {
                query = query.Where(v => v.VehicleId == vehicleId.Value);
            }

            var vehicles = await query.ToListAsync();
            var report = new List<FuelConsumptionReportViewModel>();

            foreach (var v in vehicles)
            {
                var logsQuery = v.FuelLogs.AsEnumerable();
                if (fromDate.HasValue) logsQuery = logsQuery.Where(f => f.FuelDate >= fromDate.Value);
                if (toDate.HasValue) logsQuery = logsQuery.Where(f => f.FuelDate <= toDate.Value.AddDays(1).AddTicks(-1));

                var logs = logsQuery.OrderByDescending(f => f.FuelDate).ThenByDescending(f => f.FuelLogId).ToList();

                decimal avgMileage = 0m;
                if (logs.Count >= 2)
                {
                    var sortedLogs = logs.OrderBy(f => f.OdometerReading).ToList();
                    var minOdo = sortedLogs.First().OdometerReading;
                    var maxOdo = sortedLogs.Last().OdometerReading;
                    var totalLitresExcludingFirst = sortedLogs.Skip(1).Sum(f => f.FuelQuantity);

                    if (totalLitresExcludingFirst > 0 && maxOdo > minOdo)
                    {
                        avgMileage = Math.Round((maxOdo - minOdo) / totalLitresExcludingFirst, 2);
                    }
                }

                report.Add(new FuelConsumptionReportViewModel
                {
                    VehicleId = v.VehicleId,
                    RegistrationNumber = v.RegistrationNumber,
                    VehicleName = v.VehicleName,
                    FuelType = v.FuelType,
                    RefillCount = logs.Count,
                    TotalLitres = logs.Sum(l => l.FuelQuantity),
                    TotalFuelCost = logs.Sum(l => l.TotalCost),
                    AverageMileageKmPerLitre = avgMileage,
                    FuelLogs = logs
                });
            }

            return report.OrderByDescending(r => r.TotalFuelCost);
        }
    }
}
