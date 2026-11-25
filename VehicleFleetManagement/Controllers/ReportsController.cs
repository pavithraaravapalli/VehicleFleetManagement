using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VehicleFleetManagement.Data;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.Services.Interfaces;
using VehicleFleetManagement.ViewModels;

namespace VehicleFleetManagement.Controllers
{
    /// <summary>
    /// Executive Reporting Controller for Fleet Operations & Business Intelligence.
    /// Exposes 7 operational reports, search/date filters, and export/print capability.
    /// Restricted strictly to Admin role.
    /// </summary>
    [Authorize(Roles = DbSeeder.RoleAdmin)]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly ApplicationDbContext _context;

        public ReportsController(IReportService reportService, ApplicationDbContext context)
        {
            _reportService = reportService;
            _context = context;
        }

        // ==========================================
        // GET: /Reports (Report Center Hub)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Executive Fleet Reports & Analytics";
            var stats = await _reportService.GetAdminDashboardStatsAsync();
            return View(stats);
        }

        // ==========================================
        // 1. GET: /Reports/VehicleUtilization
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> VehicleUtilization(DateTime? fromDate, DateTime? toDate)
        {
            ViewBag.Title = "Vehicle Utilization & Mileage Report";
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            var report = await _reportService.GetVehicleUtilizationReportAsync(fromDate, toDate);
            return View(report);
        }

        // ==========================================
        // 2. GET: /Reports/DriverTripHistory
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> DriverTripHistory(int? driverId, DateTime? fromDate, DateTime? toDate)
        {
            ViewBag.Title = "Driver Assignment & Performance History";
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SelectedDriverId = driverId;

            ViewBag.DriversList = new SelectList(
                await _context.Drivers.OrderBy(d => d.FullName).Select(d => new { d.DriverId, d.FullName }).ToListAsync(),
                "DriverId",
                "FullName",
                driverId);

            var report = await _reportService.GetDriverTripHistoryReportAsync(driverId, fromDate, toDate);
            return View(report);
        }

        // ==========================================
        // 3. GET: /Reports/CompletedTrips
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> CompletedTrips(DateTime? fromDate, DateTime? toDate)
        {
            ViewBag.Title = "Completed Trips Audit Report";
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            var report = await _reportService.GetTripsReportAsync(TripStatusEnum.Completed, fromDate, toDate);
            return View(report);
        }

        // ==========================================
        // 4. GET: /Reports/PendingTrips
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> PendingTrips(DateTime? fromDate, DateTime? toDate)
        {
            ViewBag.Title = "Pending & Scheduled Trips Dispatch Report";
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            var report = await _reportService.GetTripsReportAsync(TripStatusEnum.Scheduled, fromDate, toDate);
            return View(report);
        }

        // ==========================================
        // 5. GET: /Reports/MonthlyFuelExpenses
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MonthlyFuelExpenses(int? year)
        {
            int selectedYear = year ?? DateTime.UtcNow.Year;
            ViewBag.Title = $"Monthly Fuel Expenses Report ({selectedYear})";
            ViewBag.SelectedYear = selectedYear;

            var report = await _reportService.GetMonthlyFuelExpensesReportAsync(selectedYear);
            return View(report);
        }

        // ==========================================
        // 6. GET: /Reports/MaintenanceExpenses
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MaintenanceExpenses(DateTime? fromDate, DateTime? toDate, int? vehicleId)
        {
            ViewBag.Title = "Maintenance & Servicing Expenses Report";
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SelectedVehicleId = vehicleId;

            ViewBag.VehiclesList = new SelectList(
                await _context.Vehicles.OrderBy(v => v.VehicleName).Select(v => new { v.VehicleId, Display = $"{v.VehicleName} ({v.RegistrationNumber})" }).ToListAsync(),
                "VehicleId",
                "Display",
                vehicleId);

            var report = await _reportService.GetMaintenanceExpenseReportAsync(fromDate, toDate, vehicleId);
            return View(report);
        }

        // ==========================================
        // 7. GET: /Reports/FuelConsumption
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> FuelConsumption(DateTime? fromDate, DateTime? toDate, int? vehicleId)
        {
            ViewBag.Title = "Vehicle Fuel Consumption & Mileage Analysis";
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SelectedVehicleId = vehicleId;

            ViewBag.VehiclesList = new SelectList(
                await _context.Vehicles.OrderBy(v => v.VehicleName).Select(v => new { v.VehicleId, Display = $"{v.VehicleName} ({v.RegistrationNumber})" }).ToListAsync(),
                "VehicleId",
                "Display",
                vehicleId);

            var report = await _reportService.GetFuelConsumptionReportAsync(fromDate, toDate, vehicleId);
            return View(report);
        }
    }
}
