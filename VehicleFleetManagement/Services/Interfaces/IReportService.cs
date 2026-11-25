using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleFleetManagement.ViewModels;

namespace VehicleFleetManagement.Services.Interfaces
{
    /// <summary>
    /// Service contract for aggregated business intelligence reporting,
    /// executive dashboard analytics, and operational audit reports.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Retrieves real-time executive dashboard KPIs, fleet status counts, recent activity, and chart datasets.
        /// </summary>
        Task<AdminDashboardViewModel> GetAdminDashboardStatsAsync();

        /// <summary>
        /// Generates the Vehicle Utilization report detailing trips, distances, and asset operational percentage.
        /// </summary>
        Task<IEnumerable<VehicleUtilizationReportViewModel>> GetVehicleUtilizationReportAsync(DateTime? fromDate, DateTime? toDate);

        /// <summary>
        /// Generates the Driver Trip History report tracking completed/in-progress/cancelled trips and driven distance.
        /// </summary>
        Task<IEnumerable<DriverTripHistoryReportViewModel>> GetDriverTripHistoryReportAsync(int? driverId, DateTime? fromDate, DateTime? toDate);

        /// <summary>
        /// Generates trips report filtered by status (Completed, Scheduled/Pending, InProgress, Cancelled) and date.
        /// </summary>
        Task<TripsReportViewModel> GetTripsReportAsync(string? status, DateTime? fromDate, DateTime? toDate);

        /// <summary>
        /// Generates aggregated monthly fuel expenditure and volume trends for a given calendar year.
        /// </summary>
        Task<MonthlyFuelExpensesReportViewModel> GetMonthlyFuelExpensesReportAsync(int year);

        /// <summary>
        /// Generates maintenance cost breakdown categorized by vehicle and service date range.
        /// </summary>
        Task<IEnumerable<MaintenanceExpenseReportViewModel>> GetMaintenanceExpenseReportAsync(DateTime? fromDate, DateTime? toDate, int? vehicleId);

        /// <summary>
        /// Generates fuel consumption metrics, refill volumes, costs, and mileage efficiency (km/L) per vehicle.
        /// </summary>
        Task<IEnumerable<FuelConsumptionReportViewModel>> GetFuelConsumptionReportAsync(DateTime? fromDate, DateTime? toDate, int? vehicleId);
    }
}
