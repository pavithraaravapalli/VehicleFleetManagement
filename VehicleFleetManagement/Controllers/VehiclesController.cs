using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VehicleFleetManagement.Data;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.Services.Interfaces;

namespace VehicleFleetManagement.Controllers
{
    /// <summary>
    /// Thin MVC Controller managing Vehicle CRUD workflows, filtering,
    /// pagination, and session state persistence.
    /// </summary>
    [Authorize(Roles = $"{DbSeeder.RoleAdmin},{DbSeeder.RoleUser}")]
    public class VehiclesController : Controller
    {
        private readonly IVehicleService _vehicleService;
        private const int PageSize = 6;

        public VehiclesController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        // ==========================================
        // GET: /Vehicles
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? vehicleType, string? status, int page = 1)
        {
            // INT 402 Session Demonstration: Store/restore user's active filter preference
            if (string.IsNullOrEmpty(vehicleType) && HttpContext.Session.GetString("VehicleFilter_Type") != null)
            {
                vehicleType = HttpContext.Session.GetString("VehicleFilter_Type");
            }
            else if (!string.IsNullOrEmpty(vehicleType))
            {
                HttpContext.Session.SetString("VehicleFilter_Type", vehicleType);
            }

            var totalCount = await _vehicleService.GetTotalVehicleCountAsync(searchTerm, vehicleType, status);
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var vehicles = await _vehicleService.GetAllVehiclesAsync(searchTerm, vehicleType, status, page, PageSize);

            // Populate ViewBag for pagination and active search criteria
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedType = vehicleType;
            ViewBag.SelectedStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.Title = "Vehicle Fleet Inventory";

            // Populate ViewData with SelectLists for dynamic filter controls
            var types = await _vehicleService.GetDistinctVehicleTypesAsync();
            ViewData["VehicleTypes"] = new SelectList(types);

            return View(vehicles);
        }

        // ==========================================
        // GET: /Vehicles/Details/5
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest("Vehicle identifier is required.");

            var vehicle = await _vehicleService.GetVehicleWithHistoryAsync(id.Value);
            if (vehicle == null) return NotFound($"Vehicle with ID {id} was not found.");

            ViewBag.Title = $"{vehicle.VehicleName} ({vehicle.RegistrationNumber}) - Details";
            return View(vehicle);
        }

        // ==========================================
        // GET: /Vehicles/Create
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public IActionResult Create()
        {
            ViewBag.Title = "Register New Fleet Vehicle";
            return View(new Vehicle
            {
                ManufacturingYear = DateTime.UtcNow.Year,
                RegistrationDate = DateTime.UtcNow,
                InsuranceExpiryDate = DateTime.UtcNow.AddYears(1),
                Status = VehicleStatus.Available
            });
        }

        // ==========================================
        // POST: /Vehicles/Create
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vehicle vehicle)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Register New Fleet Vehicle";
                return View(vehicle);
            }

            var (success, message) = await _vehicleService.CreateVehicleAsync(vehicle);
            if (!success)
            {
                ModelState.AddModelError(nameof(vehicle.RegistrationNumber), message);
                ViewBag.Title = "Register New Fleet Vehicle";
                return View(vehicle);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: /Vehicles/Edit/5
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();

            var vehicle = await _vehicleService.GetVehicleByIdAsync(id.Value);
            if (vehicle == null) return NotFound();

            ViewBag.Title = $"Edit Vehicle - {vehicle.RegistrationNumber}";
            return View(vehicle);
        }

        // ==========================================
        // POST: /Vehicles/Edit/5
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Vehicle vehicle)
        {
            if (id != vehicle.VehicleId) return BadRequest("Identifier mismatch.");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = $"Edit Vehicle - {vehicle.RegistrationNumber}";
                return View(vehicle);
            }

            var (success, message) = await _vehicleService.UpdateVehicleAsync(vehicle);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                ViewBag.Title = $"Edit Vehicle - {vehicle.RegistrationNumber}";
                return View(vehicle);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: /Vehicles/Delete/5
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();

            var vehicle = await _vehicleService.GetVehicleByIdAsync(id.Value);
            if (vehicle == null) return NotFound();

            ViewBag.Title = $"Delete Vehicle - {vehicle.RegistrationNumber}";
            return View(vehicle);
        }

        // ==========================================
        // POST: /Vehicles/Delete/5
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (success, message) = await _vehicleService.DeleteVehicleAsync(id);
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
