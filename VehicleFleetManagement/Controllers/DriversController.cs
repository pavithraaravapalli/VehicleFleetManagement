using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VehicleFleetManagement.Data;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.Services.Interfaces;
using VehicleFleetManagement.ViewModels;

namespace VehicleFleetManagement.Controllers
{
    /// <summary>
    /// MVC Controller managing Driver records, credential compliance,
    /// safe deactivation, trip assignment history, professional profiles,
    /// photo upload, ratings, and availability status.
    /// </summary>
    [Authorize]
    public class DriversController : Controller
    {
        private readonly IDriverService _driverService;
        private readonly IDriverExperienceService _driverExperienceService;
        private readonly IWebHostEnvironment _env;
        private const int PageSize = 6;

        public DriversController(
            IDriverService driverService,
            IDriverExperienceService driverExperienceService,
            IWebHostEnvironment env)
        {
            _driverService = driverService;
            _driverExperienceService = driverExperienceService;
            _env = env;
        }

        // ==========================================
        // GET: /Drivers
        // ==========================================
        [HttpGet]
        [Authorize(Roles = $"{DbSeeder.RoleAdmin},{DbSeeder.RoleUser}")]
        public async Task<IActionResult> Index(string? searchTerm, string? availabilityStatus, int page = 1)
        {
            if (string.IsNullOrEmpty(availabilityStatus) && HttpContext.Session.GetString("DriverFilter_Status") != null)
            {
                availabilityStatus = HttpContext.Session.GetString("DriverFilter_Status");
            }
            else if (!string.IsNullOrEmpty(availabilityStatus))
            {
                HttpContext.Session.SetString("DriverFilter_Status", availabilityStatus);
            }

            var totalCount = await _driverService.GetTotalDriverCountAsync(searchTerm, availabilityStatus);
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var drivers = await _driverService.GetAllDriversAsync(searchTerm, availabilityStatus, page, PageSize);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedStatus = availabilityStatus;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.Title = "Driver Registry & Credentials";

            return View(drivers);
        }

        // ==========================================
        // GET: /Drivers/Details/5
        // ==========================================
        [HttpGet]
        [Authorize(Roles = $"{DbSeeder.RoleAdmin},{DbSeeder.RoleUser}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest("Driver identifier is required.");

            var driver = await _driverService.GetDriverWithHistoryAsync(id.Value);
            if (driver == null) return NotFound($"Driver with ID {id} was not found.");

            ViewBag.Title = $"{driver.FullName} - Driver Profile & Trip History";
            return View(driver);
        }

        // ==========================================
        // GET: /Drivers/Profile/5
        // Dedicated Professional Profile Page
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Profile(int? id)
        {
            DriverProfileViewModel? model = null;

            if (id.HasValue && id.Value > 0)
            {
                model = await _driverExperienceService.GetDriverProfileAsync(id.Value);
            }
            else
            {
                // Fallback to currently logged in driver
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    model = await _driverExperienceService.GetDriverProfileByUserIdAsync(userId);
                }
            }

            if (model == null)
            {
                TempData["Error"] = "Driver profile not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Security check: If caller is a Driver, they can only view/edit their own profile
            if (User.IsInRole(DbSeeder.RoleDriver) && !User.IsInRole(DbSeeder.RoleAdmin))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (model.UserId != userId)
                {
                    TempData["Error"] = "Access denied. You cannot view another driver's private profile.";
                    return RedirectToAction("Driver", "Dashboard");
                }
            }

            ViewBag.Title = $"{model.FullName} - Professional Profile";
            return View(model);
        }

        // ==========================================
        // POST: /Drivers/Profile
        // Updates personal details and profile photo
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(DriverProfileViewModel model)
        {
            // Security check: Drivers can only edit their own profile
            if (User.IsInRole(DbSeeder.RoleDriver) && !User.IsInRole(DbSeeder.RoleAdmin))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var existing = await _driverExperienceService.GetDriverProfileAsync(model.DriverId);
                if (existing == null || existing.UserId != userId)
                {
                    return Forbid();
                }
            }

            var (success, message) = await _driverExperienceService.UpdateDriverProfileAsync(model, model.PhotoUpload, _env.WebRootPath);
            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Profile), new { id = model.DriverId });
        }

        // ==========================================
        // POST: /Drivers/RemovePhoto
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoto(int driverId)
        {
            if (User.IsInRole(DbSeeder.RoleDriver) && !User.IsInRole(DbSeeder.RoleAdmin))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var existing = await _driverExperienceService.GetDriverProfileAsync(driverId);
                if (existing == null || existing.UserId != userId)
                {
                    return Forbid();
                }
            }

            var (success, message) = await _driverExperienceService.RemoveProfilePictureAsync(driverId, _env.WebRootPath);
            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Profile), new { id = driverId });
        }

        // ==========================================
        // POST: /Drivers/UpdateAvailability
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvailability(int driverId, string status, string? reason)
        {
            if (User.IsInRole(DbSeeder.RoleDriver) && !User.IsInRole(DbSeeder.RoleAdmin))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var existing = await _driverExperienceService.GetDriverProfileAsync(driverId);
                if (existing == null || existing.UserId != userId)
                {
                    return Forbid();
                }
            }

            var (success, message) = await _driverExperienceService.UpdateAvailabilityStatusAsync(driverId, status, reason);
            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            // Return to driver dashboard or profile
            if (User.IsInRole(DbSeeder.RoleDriver))
            {
                return RedirectToAction("Driver", "Dashboard");
            }

            return RedirectToAction(nameof(Profile), new { id = driverId });
        }

        // ==========================================
        // POST: /Drivers/SubmitReview
        // Allows passengers/staff to submit a 5-star review for an eligible completed trip
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(SubmitDriverReviewViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            // Drivers cannot review themselves
            var currentDriver = await _driverExperienceService.GetDriverProfileByUserIdAsync(userId);
            if (currentDriver != null && currentDriver.DriverId == model.DriverId)
            {
                TempData["Error"] = "Drivers are not permitted to submit reviews for their own journeys.";
                return RedirectToAction("Details", "Trips", new { id = model.TripId });
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please provide both a star rating and feedback comment.";
                return RedirectToAction("Details", "Trips", new { id = model.TripId });
            }

            var (success, message) = await _driverExperienceService.SubmitDriverReviewAsync(model, userId);
            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction("Details", "Trips", new { id = model.TripId });
        }

        // ==========================================
        // POST: /Drivers/ModerateReview
        // Admin review moderation
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModerateReview(int reviewId, int driverId, bool hideReview)
        {
            var (success, message) = await _driverExperienceService.ModerateReviewAsync(reviewId, hideReview);
            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Profile), new { id = driverId });
        }

        // ==========================================
        // GET: /Drivers/Create
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public IActionResult Create()
        {
            ViewBag.Title = "Register New Fleet Driver";
            return View(new Driver
            {
                JoiningDate = DateTime.UtcNow,
                LicenseExpiryDate = DateTime.UtcNow.AddYears(3),
                AvailabilityStatus = DriverStatus.Available
            });
        }

        // ==========================================
        // POST: /Drivers/Create
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Driver driver)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Register New Fleet Driver";
                return View(driver);
            }

            var (success, message) = await _driverService.CreateDriverAsync(driver);
            if (!success)
            {
                ModelState.AddModelError(nameof(driver.LicenseNumber), message);
                ViewBag.Title = "Register New Fleet Driver";
                return View(driver);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: /Drivers/Edit/5
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();

            var driver = await _driverService.GetDriverByIdAsync(id.Value);
            if (driver == null) return NotFound();

            ViewBag.Title = $"Edit Driver - {driver.FullName}";
            return View(driver);
        }

        // ==========================================
        // POST: /Drivers/Edit/5
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Driver driver)
        {
            if (id != driver.DriverId) return BadRequest("Identifier mismatch.");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = $"Edit Driver - {driver.FullName}";
                return View(driver);
            }

            var (success, message) = await _driverService.UpdateDriverAsync(driver);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                ViewBag.Title = $"Edit Driver - {driver.FullName}";
                return View(driver);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: /Drivers/Delete/5
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();

            var driver = await _driverService.GetDriverByIdAsync(id.Value);
            if (driver == null) return NotFound();

            ViewBag.Title = $"Remove / Deactivate Driver - {driver.FullName}";
            return View(driver);
        }

        // ==========================================
        // POST: /Drivers/Delete/5
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (success, message) = await _driverService.DeleteOrDeactivateDriverAsync(id);
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
    }
}
