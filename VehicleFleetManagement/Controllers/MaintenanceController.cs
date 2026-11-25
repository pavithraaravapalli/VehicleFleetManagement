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
    /// Thin MVC Controller managing vehicle maintenance scheduling, repair logs,
    /// 7-day upcoming reminder detection, and workshop expenditure tracking.
    /// </summary>
    [Authorize(Roles = DbSeeder.RoleAdmin)]
    public class MaintenanceController : Controller
    {
        private readonly IMaintenanceService _maintenanceService;
        private readonly ApplicationDbContext _context;
        private const int PageSize = 8;

        public MaintenanceController(IMaintenanceService maintenanceService, ApplicationDbContext context)
        {
            _maintenanceService = maintenanceService;
            _context = context;
        }

        // ==========================================
        // GET: /Maintenance
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? status, int? vehicleId, int page = 1)
        {
            var totalCount = await _maintenanceService.GetTotalCountAsync(searchTerm, status, vehicleId);
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var records = await _maintenanceService.GetAllMaintenanceAsync(searchTerm, status, vehicleId, page, PageSize);
            var upcomingAlerts = await _maintenanceService.GetUpcomingMaintenanceAlertsAsync(7);
            var overdueAlerts = await _maintenanceService.GetOverdueMaintenanceAsync();
            var totalCost = await _maintenanceService.GetTotalMaintenanceCostAsync();

            var vehicles = await _context.Vehicles
                .OrderBy(v => v.VehicleName)
                .Select(v => new SelectListItem
                {
                    Value = v.VehicleId.ToString(),
                    Text = $"{v.VehicleName} ({v.RegistrationNumber})"
                })
                .ToListAsync();

            ViewData["Vehicles"] = new SelectList(vehicles, "Value", "Text", vehicleId);

            var viewModel = new MaintenanceIndexViewModel
            {
                Maintenances = records,
                UpcomingAlerts = upcomingAlerts,
                OverdueAlerts = overdueAlerts,
                TotalMaintenanceCost = totalCost,
                TotalCount = totalCount,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchTerm = searchTerm,
                SelectedStatus = status,
                SelectedVehicleId = vehicleId
            };

            ViewBag.Title = "Vehicle Maintenance Operations";
            return View(viewModel);
        }

        // ==========================================
        // GET: /Maintenance/Details/5
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var record = await _maintenanceService.GetByIdAsync(id.Value);
            if (record == null) return NotFound("Maintenance record not found.");

            ViewBag.Title = $"Maintenance Record #{record.MaintenanceId}";
            return View(record);
        }

        // ==========================================
        // GET: /Maintenance/Create
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create(int? vehicleId)
        {
            ViewBag.Title = "Schedule Maintenance Event";
            var model = new CreateMaintenanceViewModel
            {
                VehicleId = vehicleId ?? 0,
                ServiceDate = DateTime.UtcNow,
                NextServiceDate = DateTime.UtcNow.AddMonths(6),
                Status = MaintenanceStatus.Scheduled
            };

            await PopulateVehiclesDropdownAsync(model);
            return View(model);
        }

        // ==========================================
        // POST: /Maintenance/Create
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMaintenanceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Schedule Maintenance Event";
                await PopulateVehiclesDropdownAsync(model);
                return View(model);
            }

            var maintenance = new Maintenance
            {
                VehicleId = model.VehicleId,
                ServiceType = model.ServiceType.Trim(),
                Description = model.Description.Trim(),
                ServiceDate = model.ServiceDate,
                NextServiceDate = model.NextServiceDate,
                Cost = model.Cost,
                ServiceCenter = model.ServiceCenter.Trim(),
                Status = model.Status
            };

            var (success, message) = await _maintenanceService.CreateMaintenanceAsync(maintenance);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                ViewBag.Title = "Schedule Maintenance Event";
                await PopulateVehiclesDropdownAsync(model);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: /Maintenance/Edit/5
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();

            var record = await _maintenanceService.GetByIdAsync(id.Value);
            if (record == null) return NotFound();

            var model = new CreateMaintenanceViewModel
            {
                MaintenanceId = record.MaintenanceId,
                VehicleId = record.VehicleId,
                ServiceType = record.ServiceType,
                Description = record.Description,
                ServiceDate = record.ServiceDate,
                NextServiceDate = record.NextServiceDate,
                Cost = record.Cost,
                ServiceCenter = record.ServiceCenter,
                Status = record.Status
            };

            await PopulateVehiclesDropdownAsync(model);
            ViewBag.Title = $"Edit Maintenance #{record.MaintenanceId}";
            return View(model);
        }

        // ==========================================
        // POST: /Maintenance/Edit/5
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateMaintenanceViewModel model)
        {
            if (id != model.MaintenanceId) return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.Title = $"Edit Maintenance #{model.MaintenanceId}";
                await PopulateVehiclesDropdownAsync(model);
                return View(model);
            }

            var maintenance = new Maintenance
            {
                MaintenanceId = model.MaintenanceId,
                VehicleId = model.VehicleId,
                ServiceType = model.ServiceType.Trim(),
                Description = model.Description.Trim(),
                ServiceDate = model.ServiceDate,
                NextServiceDate = model.NextServiceDate,
                Cost = model.Cost,
                ServiceCenter = model.ServiceCenter.Trim(),
                Status = model.Status
            };

            var (success, message) = await _maintenanceService.UpdateMaintenanceAsync(maintenance);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                ViewBag.Title = $"Edit Maintenance #{model.MaintenanceId}";
                await PopulateVehiclesDropdownAsync(model);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: /Maintenance/Delete/5
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();

            var record = await _maintenanceService.GetByIdAsync(id.Value);
            if (record == null) return NotFound();

            ViewBag.Title = $"Delete Maintenance #{record.MaintenanceId}";
            return View(record);
        }

        // ==========================================
        // POST: /Maintenance/Delete/5
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (success, message) = await _maintenanceService.DeleteMaintenanceAsync(id);
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

        private async Task PopulateVehiclesDropdownAsync(CreateMaintenanceViewModel model)
        {
            model.AvailableVehicles = await _context.Vehicles
                .OrderBy(v => v.VehicleName)
                .Select(v => new SelectListItem
                {
                    Value = v.VehicleId.ToString(),
                    Text = $"{v.VehicleName} ({v.RegistrationNumber}) [{v.Status}]"
                })
                .ToListAsync();
        }
    }
}
