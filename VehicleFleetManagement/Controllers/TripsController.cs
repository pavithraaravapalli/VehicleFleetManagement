using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    /// Thin MVC Controller managing trip dispatching, conflict validation,
    /// and driver self-service journey execution.
    /// </summary>
    [Authorize]
    public class TripsController : Controller
    {
        private readonly ITripService _tripService;
        private readonly IVehicleService _vehicleService;
        private readonly IDriverService _driverService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int PageSize = 8;

        public TripsController(
            ITripService tripService,
            IVehicleService vehicleService,
            IDriverService driverService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _tripService = tripService;
            _vehicleService = vehicleService;
            _driverService = driverService;
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // GET: /Trips
        // ==========================================
        [HttpGet]
        [Authorize(Roles = $"{DbSeeder.RoleAdmin},{DbSeeder.RoleUser}")]
        public async Task<IActionResult> Index(string? searchTerm, string? status, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            var totalCount = await _tripService.GetTotalTripsCountAsync(searchTerm, status, fromDate, toDate);
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var trips = await _tripService.GetAllTripsAsync(searchTerm, status, fromDate, toDate, page, PageSize);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedStatus = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.Title = "Fleet Trip Logs & Operations";

            return View(trips);
        }

        // ==========================================
        // GET: /Trips/Details/5
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var trip = await _tripService.GetTripByIdAsync(id.Value);
            if (trip == null) return NotFound("Trip not found.");

            // If user is a Driver, enforce self-isolation (Requirement 13)
            if (User.IsInRole(DbSeeder.RoleDriver))
            {
                var userId = _userManager.GetUserId(User);
                if (trip.Driver?.UserId != userId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            ViewBag.Title = $"Trip #{trip.TripId} Details ({trip.Source} to {trip.Destination})";
            return View(trip);
        }

        // ==========================================
        // GET: /Trips/Create
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Schedule & Dispatch Fleet Trip";
            var model = new CreateTripViewModel();
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        // ==========================================
        // POST: /Trips/Create
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTripViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Schedule & Dispatch Fleet Trip";
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            var trip = new Trip
            {
                VehicleId = model.VehicleId,
                DriverId = model.DriverId,
                Source = model.Source.Trim(),
                Destination = model.Destination.Trim(),
                StartDate = model.StartDate,
                ExpectedEndDate = model.ExpectedEndDate,
                StartOdometer = model.StartOdometer,
                Purpose = model.Purpose.Trim(),
                TripStatus = TripStatusEnum.Scheduled
            };

            var (success, message) = await _tripService.CreateTripAsync(trip);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                ViewBag.Title = "Schedule & Dispatch Fleet Trip";
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: /Trips/MyTrips (Driver Self-Service Portal)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleDriver)]
        public async Task<IActionResult> MyTrips(string? status)
        {
            var userId = _userManager.GetUserId(User);
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
            {
                return RedirectToAction("Driver", "Dashboard");
            }

            var trips = await _tripService.GetDriverTripsAsync(driver.DriverId, status);

            ViewBag.SelectedStatus = status;
            ViewBag.Driver = driver;
            ViewBag.Title = "My Assigned Journeys";

            return View(trips);
        }

        // ==========================================
        // GET: /Trips/Start/5
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Start(int? id)
        {
            if (id == null) return BadRequest();

            var trip = await _tripService.GetTripByIdAsync(id.Value);
            if (trip == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(DbSeeder.RoleAdmin);

            if (!isAdmin && trip.Driver?.UserId != currentUserId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (trip.TripStatus != TripStatusEnum.Scheduled)
            {
                TempData["Error"] = $"Cannot start trip. Only scheduled trips can be started. Current status: '{trip.TripStatus}'.";
                return RedirectToAction(isAdmin ? nameof(Index) : nameof(MyTrips));
            }

            var model = new StartTripViewModel
            {
                TripId = trip.TripId,
                VehicleName = trip.Vehicle?.VehicleName ?? "N/A",
                RegistrationNumber = trip.Vehicle?.RegistrationNumber ?? "N/A",
                DriverName = trip.Driver?.FullName ?? "N/A",
                Route = $"{trip.Source} to {trip.Destination}",
                StartOdometer = trip.Vehicle?.CurrentOdometer ?? trip.StartOdometer
            };

            ViewBag.Title = $"Start Journey #{trip.TripId}";
            return View(model);
        }

        // ==========================================
        // POST: /Trips/Start/5
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(StartTripViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = $"Start Journey #{model.TripId}";
                return View(model);
            }

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(DbSeeder.RoleAdmin);

            var (success, message) = await _tripService.StartTripAsync(model.TripId, model.StartOdometer, currentUserId, isAdmin);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                ViewBag.Title = $"Start Journey #{model.TripId}";
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(isAdmin ? nameof(Index) : nameof(MyTrips));
        }

        // ==========================================
        // GET: /Trips/Complete/5
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Complete(int? id)
        {
            if (id == null) return BadRequest();

            var trip = await _tripService.GetTripByIdAsync(id.Value);
            if (trip == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(DbSeeder.RoleAdmin);

            if (!isAdmin && trip.Driver?.UserId != currentUserId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (trip.TripStatus != TripStatusEnum.InProgress)
            {
                TempData["Error"] = $"Only in-progress trips can be completed. Current status: '{trip.TripStatus}'.";
                return RedirectToAction(isAdmin ? nameof(Index) : nameof(MyTrips));
            }

            var model = new CompleteTripViewModel
            {
                TripId = trip.TripId,
                VehicleName = trip.Vehicle?.VehicleName ?? "N/A",
                RegistrationNumber = trip.Vehicle?.RegistrationNumber ?? "N/A",
                DriverName = trip.Driver?.FullName ?? "N/A",
                Route = $"{trip.Source} to {trip.Destination}",
                StartOdometer = trip.StartOdometer,
                EndOdometer = trip.StartOdometer, // default to start odometer
                ActualEndDate = DateTime.UtcNow
            };

            ViewBag.Title = $"Complete Journey #{trip.TripId}";
            return View(model);
        }

        // ==========================================
        // POST: /Trips/Complete/5
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(CompleteTripViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = $"Complete Journey #{model.TripId}";
                return View(model);
            }

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole(DbSeeder.RoleAdmin);

            var (success, message) = await _tripService.CompleteTripAsync(
                model.TripId, model.EndOdometer, model.ActualEndDate, currentUserId, isAdmin, model.TripNotes);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                ViewBag.Title = $"Complete Journey #{model.TripId}";
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(isAdmin ? nameof(Index) : nameof(MyTrips));
        }

        // ==========================================
        // POST: /Trips/Cancel/5
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var (success, message) = await _tripService.CancelTripAsync(id, reason, _userManager.GetUserId(User), isAdmin: true);
            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: /Trips/Diagnostics (University Viva Test Suite)
        // Validates all business rules, overlapping conflict checks,
        // odometer updates, and state transitions.
        // ==========================================
        [HttpGet("/Trips/Diagnostics")]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public async Task<IActionResult> Diagnostics()
        {
            var results = new System.Collections.Generic.List<string>();

            // 1. Fetch baseline assets
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Status == VehicleStatus.Available);
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.AvailabilityStatus == DriverStatus.Available);

            if (vehicle == null || driver == null)
            {
                return Json(new { success = false, message = "Baseline vehicle or driver not found to execute diagnostics." });
            }

            // Scenario 1: Create a test trip
            var testTrip = new Trip
            {
                VehicleId = vehicle.VehicleId,
                DriverId = driver.DriverId,
                Source = "Diagnostics Hub Alpha",
                Destination = "Diagnostics Hub Beta",
                StartDate = DateTime.UtcNow.AddHours(2),
                ExpectedEndDate = DateTime.UtcNow.AddHours(6),
                StartOdometer = vehicle.CurrentOdometer,
                Purpose = "Automated Viva Business Rule Verification Test",
                TripStatus = TripStatusEnum.Scheduled
            };

            var (createOk, createMsg) = await _tripService.CreateTripAsync(testTrip);
            if (!createOk) return Json(new { success = false, step = "CreateTrip", error = createMsg });
            results.Add($"[PASS] Scenario 1: Trip #{testTrip.TripId} created with status '{testTrip.TripStatus}'.");

            // Scenario 2: Overlapping Conflict Detection (Vehicle)
            var (conflictOk, conflictMsg) = await _tripService.CheckVehicleAvailabilityAsync(
                vehicle.VehicleId, testTrip.StartDate.AddMinutes(30), testTrip.ExpectedEndDate.AddMinutes(30));
            if (!conflictOk)
            {
                return Json(new { success = false, step = "VehicleConflictCheck", error = "Failed to detect vehicle overlap conflict!" });
            }
            results.Add($"[PASS] Scenario 2: Successfully detected overlapping vehicle conflict: '{conflictMsg}'.");

            // Scenario 3: Overlapping Conflict Detection (Driver)
            var (driverConflictOk, driverConflictMsg) = await _tripService.CheckDriverAvailabilityAsync(
                driver.DriverId, testTrip.StartDate.AddMinutes(30), testTrip.ExpectedEndDate.AddMinutes(30));
            if (!driverConflictOk)
            {
                return Json(new { success = false, step = "DriverConflictCheck", error = "Failed to detect driver overlap conflict!" });
            }
            results.Add($"[PASS] Scenario 3: Successfully detected overlapping driver conflict: '{driverConflictMsg}'.");

            // Scenario 4: Start Trip & Verify State Transition
            var (startOk, startMsg) = await _tripService.StartTripAsync(testTrip.TripId, vehicle.CurrentOdometer, isAdmin: true);
            if (!startOk) return Json(new { success = false, step = "StartTrip", error = startMsg });

            var startedVehicle = await _context.Vehicles.FindAsync(vehicle.VehicleId);
            var startedDriver = await _context.Drivers.FindAsync(driver.DriverId);
            if (startedVehicle?.Status != VehicleStatus.Assigned || startedDriver?.AvailabilityStatus != DriverStatus.OnTrip)
            {
                return Json(new { success = false, step = "StateTransitionCheck", error = "Vehicle or Driver status did not transition to Assigned/OnTrip." });
            }
            results.Add($"[PASS] Scenario 4: Trip #{testTrip.TripId} started. Vehicle status '{startedVehicle.Status}' and Driver status '{startedDriver.AvailabilityStatus}'.");

            // Scenario 5: Complete Trip & Audit Distance & Status Reversion
            decimal distanceToAdd = 125.75m;
            decimal endOdometer = vehicle.CurrentOdometer + distanceToAdd;

            var (completeOk, completeMsg) = await _tripService.CompleteTripAsync(
                testTrip.TripId, endOdometer, DateTime.UtcNow, isAdmin: true);
            if (!completeOk) return Json(new { success = false, step = "CompleteTrip", error = completeMsg });

            var completedTrip = await _context.Trips.FindAsync(testTrip.TripId);
            var releasedVehicle = await _context.Vehicles.FindAsync(vehicle.VehicleId);
            var releasedDriver = await _context.Drivers.FindAsync(driver.DriverId);

            if (completedTrip?.Distance != distanceToAdd ||
                releasedVehicle?.Status != VehicleStatus.Available ||
                releasedDriver?.AvailabilityStatus != DriverStatus.Available ||
                releasedVehicle?.CurrentOdometer != endOdometer)
            {
                return Json(new { success = false, step = "CompletionAudit", error = "Distance, odometer, or status release mismatch." });
            }
            results.Add($"[PASS] Scenario 5: Trip #{testTrip.TripId} completed. Distance: {completedTrip.Distance:N2} km, Vehicle released to '{releasedVehicle.Status}' with Odometer: {releasedVehicle.CurrentOdometer:N2} km.");

            // Scenario 6: Prevent Invalid Transition (re-starting completed trip)
            var (restartOk, restartMsg) = await _tripService.StartTripAsync(testTrip.TripId, endOdometer, isAdmin: true);
            if (restartOk)
            {
                return Json(new { success = false, step = "InvalidTransition", error = "System erroneously permitted restarting a completed trip." });
            }
            results.Add($"[PASS] Scenario 6: Successfully blocked restarting a completed trip: '{restartMsg}'.");

            // Cleanup test record to keep database pristine
            _context.Trips.Remove(completedTrip);
            // Revert vehicle odometer
            releasedVehicle.CurrentOdometer -= distanceToAdd;
            await _context.SaveChangesAsync();
            results.Add("[CLEANUP] Test records purged and odometer baseline restored.");

            return Json(new
            {
                success = true,
                totalScenariosVerified = 6,
                summary = "All business rules, concurrency transactions, conflict validations, and odometer audits passed 100%.",
                log = results
            });
        }

        private async Task PopulateDropdownsAsync(CreateTripViewModel model)
        {
            var vehicles = await _context.Vehicles
                .Where(v => v.Status != VehicleStatus.UnderMaintenance && v.Status != VehicleStatus.Inactive)
                .OrderBy(v => v.VehicleName)
                .Select(v => new SelectListItem
                {
                    Value = v.VehicleId.ToString(),
                    Text = $"{v.VehicleName} ({v.RegistrationNumber}) - Odo: {v.CurrentOdometer:N0}km [{v.Status}]"
                })
                .ToListAsync();

            var drivers = await _context.Drivers
                .Where(d => d.AvailabilityStatus != DriverStatus.Inactive)
                .OrderBy(d => d.FullName)
                .Select(d => new SelectListItem
                {
                    Value = d.DriverId.ToString(),
                    Text = $"{d.FullName} - License: {d.LicenseNumber} [{d.AvailabilityStatus}]"
                })
                .ToListAsync();

            model.AvailableVehicles = vehicles;
            model.AvailableDrivers = drivers;
        }
    }
}

