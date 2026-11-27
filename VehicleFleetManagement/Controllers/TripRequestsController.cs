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
    [Authorize]
    public class TripRequestsController : Controller
    {
        private readonly ITripRequestService _tripRequestService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TripRequestsController(
            ITripRequestService tripRequestService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _tripRequestService = tripRequestService;
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // GET: /TripRequests
        // User view of their own transport requests
        // ==========================================
        [HttpGet]
        [Authorize(Roles = $"{DbSeeder.RoleUser},{DbSeeder.RoleAdmin}")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (await _userManager.IsInRoleAsync(user, DbSeeder.RoleAdmin))
            {
                return RedirectToAction(nameof(Manage));
            }

            ViewBag.Title = "My Transport Requests";
            var requests = await _tripRequestService.GetUserTripRequestsAsync(user.Id);
            return View(requests);
        }

        // ==========================================
        // GET: /TripRequests/Create
        // User booking form
        // ==========================================
        [HttpGet]
        [Authorize(Roles = $"{DbSeeder.RoleUser},{DbSeeder.RoleAdmin}")]
        public IActionResult Create()
        {
            ViewBag.Title = "Submit Transport Booking Request";
            var model = new CreateTripRequestViewModel
            {
                StartDate = DateTime.UtcNow.AddHours(2),
                EstimatedEndDate = DateTime.UtcNow.AddHours(6)
            };
            return View(model);
        }

        // ==========================================
        // POST: /TripRequests/Create
        // ==========================================
        [HttpPost]
        [Authorize(Roles = $"{DbSeeder.RoleUser},{DbSeeder.RoleAdmin}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTripRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Submit Transport Booking Request";
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var tripRequest = new TripRequest
            {
                UserId = user.Id,
                Source = model.Source.Trim(),
                Destination = model.Destination.Trim(),
                StartDate = model.StartDate,
                EstimatedEndDate = model.EstimatedEndDate,
                PassengerCount = model.PassengerCount,
                PreferredVehicleType = model.PreferredVehicleType,
                Purpose = model.Purpose.Trim(),
                Status = TripRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var (success, message) = await _tripRequestService.CreateTripRequestAsync(tripRequest);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                ViewBag.Title = "Submit Transport Booking Request";
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: /TripRequests/Details/5
        // ==========================================
        [HttpGet]
        [Authorize(Roles = $"{DbSeeder.RoleUser},{DbSeeder.RoleAdmin}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest("Request identifier is required.");

            var request = await _tripRequestService.GetTripRequestByIdAsync(id.Value);
            if (request == null) return NotFound("Trip request not found.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var isAdmin = await _userManager.IsInRoleAsync(user, DbSeeder.RoleAdmin);
            if (!isAdmin && request.UserId != user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            ViewBag.Title = $"Transport Request #{request.TripRequestId} Details";
            return View(request);
        }

        // ==========================================
        // POST: /TripRequests/Cancel/5
        // ==========================================
        [HttpPost]
        [Authorize(Roles = $"{DbSeeder.RoleUser},{DbSeeder.RoleAdmin}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var (success, message) = await _tripRequestService.CancelTripRequestAsync(id, user.Id);
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
        // GET: /TripRequests/Manage
        // Admin overview of all booking requests
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public async Task<IActionResult> Manage(string? status, int page = 1)
        {
            const int pageSize = 10;
            var totalCount = await _tripRequestService.GetTripRequestsCountAsync(status);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var requests = await _tripRequestService.GetAllTripRequestsAsync(status, page, pageSize);

            ViewBag.SelectedStatus = status ?? "All";
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PendingCount = await _tripRequestService.GetPendingCountAsync();
            ViewBag.Title = "Manage Fleet Transport Requests";

            return View(requests);
        }

        // ==========================================
        // GET: /TripRequests/Approve/5
        // Admin review & vehicle/driver assignment
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public async Task<IActionResult> Approve(int? id)
        {
            if (id == null) return BadRequest("Request identifier is required.");

            var request = await _tripRequestService.GetTripRequestByIdAsync(id.Value);
            if (request == null) return NotFound("Trip request not found.");

            if (request.Status != TripRequestStatus.Pending)
            {
                TempData["Error"] = $"Request #{request.TripRequestId} is already {request.Status}.";
                return RedirectToAction(nameof(Manage));
            }

            var availableVehicles = await _context.Vehicles
                .Where(v => v.Status == VehicleStatus.Available)
                .OrderBy(v => v.VehicleName)
                .Select(v => new SelectListItem
                {
                    Value = v.VehicleId.ToString(),
                    Text = $"{v.VehicleName} ({v.RegistrationNumber}) - {v.VehicleType} [{v.SeatingCapacity} seats]"
                })
                .ToListAsync();

            var availableDrivers = await _context.Drivers
                .Where(d => d.AvailabilityStatus == DriverStatus.Available)
                .OrderBy(d => d.FullName)
                .Select(d => new SelectListItem
                {
                    Value = d.DriverId.ToString(),
                    Text = $"{d.FullName} - Lic: {d.LicenseNumber} (★ {d.Rating:F1})"
                })
                .ToListAsync();

            var model = new ApproveTripRequestViewModel
            {
                TripRequestId = request.TripRequestId,
                RequesterName = request.User?.FullName ?? "Unknown",
                RequesterEmail = request.User?.Email ?? "Unknown",
                Source = request.Source,
                Destination = request.Destination,
                StartDate = request.StartDate,
                EstimatedEndDate = request.EstimatedEndDate,
                PassengerCount = request.PassengerCount,
                PreferredVehicleType = request.PreferredVehicleType,
                Purpose = request.Purpose,
                CreatedAt = request.CreatedAt,
                AvailableVehicles = availableVehicles,
                AvailableDrivers = availableDrivers
            };

            ViewBag.Title = $"Approve Request #{request.TripRequestId} & Dispatch Journey";
            return View(model);
        }

        // ==========================================
        // POST: /TripRequests/Approve
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(ApproveTripRequestViewModel model)
        {
            if (model.SelectedVehicleId <= 0 || model.SelectedDriverId <= 0)
            {
                ModelState.AddModelError(string.Empty, "Please select both an available vehicle and an available driver.");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableVehicles = await _context.Vehicles
                    .Where(v => v.Status == VehicleStatus.Available)
                    .Select(v => new SelectListItem { Value = v.VehicleId.ToString(), Text = $"{v.VehicleName} ({v.RegistrationNumber})" })
                    .ToListAsync();

                model.AvailableDrivers = await _context.Drivers
                    .Where(d => d.AvailabilityStatus == DriverStatus.Available)
                    .Select(d => new SelectListItem { Value = d.DriverId.ToString(), Text = $"{d.FullName} ({d.LicenseNumber})" })
                    .ToListAsync();

                ViewBag.Title = $"Approve Request #{model.TripRequestId}";
                return View(model);
            }

            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null) return Challenge();

            var (success, message, tripId) = await _tripRequestService.ApproveTripRequestAsync(
                model.TripRequestId,
                model.SelectedVehicleId,
                model.SelectedDriverId,
                model.AdminNotes,
                adminUser.Id);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(Approve), new { id = model.TripRequestId });
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Manage));
        }

        // ==========================================
        // POST: /TripRequests/Reject
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int tripRequestId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "A valid rejection reason must be provided.";
                return RedirectToAction(nameof(Manage));
            }

            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null) return Challenge();

            var (success, message) = await _tripRequestService.RejectTripRequestAsync(tripRequestId, reason.Trim(), adminUser.Id);
            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Manage));
        }
    }
}
