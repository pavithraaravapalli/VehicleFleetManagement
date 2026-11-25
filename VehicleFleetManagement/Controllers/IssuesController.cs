using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleFleetManagement.Data;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.Services.Interfaces;
using VehicleFleetManagement.ViewModels;

namespace VehicleFleetManagement.Controllers
{
    [Authorize]
    public class IssuesController : Controller
    {
        private readonly IDriverExperienceService _driverExperienceService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public IssuesController(
            IDriverExperienceService driverExperienceService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _driverExperienceService = driverExperienceService;
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // ==========================================
        // GET: /Issues
        // ==========================================
        public async Task<IActionResult> Index(string? status, string? severity, int? vehicleId)
        {
            ViewBag.Title = "Vehicle Fault Reports - FleetFlow Pro";
            ViewBag.StatusFilter = status;
            ViewBag.SeverityFilter = severity;
            ViewBag.VehicleIdFilter = vehicleId;

            // If Driver role, only show issues reported by this driver
            if (User.IsInRole(DbSeeder.RoleDriver) && !User.IsInRole(DbSeeder.RoleAdmin))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
                if (driver != null)
                {
                    var driverIssues = await _driverExperienceService.GetDriverIssuesAsync(driver.DriverId);
                    return View(driverIssues);
                }
            }

            var allIssues = await _driverExperienceService.GetAllIssuesAsync(status, severity, vehicleId);
            ViewBag.Vehicles = await _context.Vehicles.OrderBy(v => v.RegistrationNumber).ToListAsync();
            return View(allIssues);
        }

        // ==========================================
        // GET: /Issues/Details/5
        // ==========================================
        public async Task<IActionResult> Details(int id)
        {
            var issue = await _driverExperienceService.GetIssueByIdAsync(id);
            if (issue == null)
            {
                TempData["Error"] = "Fault report not found.";
                return RedirectToAction(nameof(Index));
            }

            // Driver authorization check: cannot view another driver's issue if not admin
            if (User.IsInRole(DbSeeder.RoleDriver) && !User.IsInRole(DbSeeder.RoleAdmin))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
                if (driver == null || driver.DriverId != issue.DriverId)
                {
                    TempData["Error"] = "You are not authorized to view this fault report.";
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Title = $"Fault Report #{issue.IssueReportId} - FleetFlow Pro";
            return View(issue);
        }

        // ==========================================
        // GET: /Issues/Create
        // ==========================================
        public async Task<IActionResult> Create(int? vehicleId, int? tripId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

            // If admin is creating or driver is creating
            var model = new CreateVehicleIssueViewModel
            {
                VehicleId = vehicleId ?? 0,
                TripId = tripId,
                DriverId = driver?.DriverId ?? 0
            };

            if (vehicleId.HasValue)
            {
                var vehicle = await _context.Vehicles.FindAsync(vehicleId.Value);
                if (vehicle != null)
                {
                    model.VehicleRegistration = vehicle.RegistrationNumber;
                    model.VehicleName = vehicle.VehicleName;
                }
            }

            ViewBag.Title = "Report Vehicle Fault - FleetFlow Pro";
            ViewBag.Vehicles = await _context.Vehicles.OrderBy(v => v.RegistrationNumber).ToListAsync();
            return View(model);
        }

        // ==========================================
        // POST: /Issues/Create
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateVehicleIssueViewModel model)
        {
            // Ensure driver ID is mapped to current user if not provided
            if (model.DriverId <= 0)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
                if (driver != null)
                {
                    model.DriverId = driver.DriverId;
                }
                else
                {
                    // If Admin reporting, use the first driver or assign admin driver record
                    var firstDriver = await _context.Drivers.FirstOrDefaultAsync();
                    model.DriverId = firstDriver?.DriverId ?? 1;
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Report Vehicle Fault - FleetFlow Pro";
                ViewBag.Vehicles = await _context.Vehicles.OrderBy(v => v.RegistrationNumber).ToListAsync();
                return View(model);
            }

            var (success, message, _) = await _driverExperienceService.ReportVehicleIssueAsync(model, _env.WebRootPath);
            if (success)
            {
                TempData["Success"] = message;
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = message;
            ViewBag.Vehicles = await _context.Vehicles.OrderBy(v => v.RegistrationNumber).ToListAsync();
            return View(model);
        }

        // ==========================================
        // POST: /Issues/UpdateStatus
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int issueReportId, string status, string? resolutionNotes)
        {
            var (success, message) = await _driverExperienceService.UpdateVehicleIssueStatusAsync(issueReportId, status, resolutionNotes);
            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Details), new { id = issueReportId });
        }
    }
}
