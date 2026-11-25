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
    /// Thin MVC Controller managing fuel purchase logging, mileage calculation,
    /// price auditing, and monthly cost aggregation.
    /// Accessible to both Admins and Operating Drivers.
    /// </summary>
    [Authorize]
    public class FuelController : Controller
    {
        private readonly IFuelService _fuelService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int PageSize = 8;

        public FuelController(
            IFuelService fuelService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _fuelService = fuelService;
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // GET: /Fuel
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(int? vehicleId, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            var totalCount = await _fuelService.GetTotalCountAsync(vehicleId, fromDate, toDate);
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var logs = await _fuelService.GetAllFuelLogsAsync(vehicleId, fromDate, toDate, page, PageSize);
            var totalCost = await _fuelService.GetTotalFuelExpensesAsync(vehicleId, fromDate, toDate);

            var totalLitres = logs.Sum(l => l.FuelQuantity);
            var avgPrice = totalLitres > 0 ? totalCost / totalLitres : 0m;

            var vehicles = await _context.Vehicles
                .OrderBy(v => v.VehicleName)
                .Select(v => new SelectListItem
                {
                    Value = v.VehicleId.ToString(),
                    Text = $"{v.VehicleName} ({v.RegistrationNumber})"
                })
                .ToListAsync();

            ViewData["Vehicles"] = new SelectList(vehicles, "Value", "Text", vehicleId);

            var viewModel = new FuelIndexViewModel
            {
                FuelLogs = logs,
                TotalFuelCost = totalCost,
                TotalLitres = totalLitres,
                AveragePricePerLitre = avgPrice,
                TotalCount = totalCount,
                CurrentPage = page,
                TotalPages = totalPages,
                SelectedVehicleId = vehicleId,
                FromDate = fromDate,
                ToDate = toDate
            };

            ViewBag.Title = "Fuel Consumption & Expense Registry";
            return View(viewModel);
        }

        // ==========================================
        // GET: /Fuel/Details/5
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var log = await _fuelService.GetByIdAsync(id.Value);
            if (log == null) return NotFound("Fuel record not found.");

            ViewBag.CalculatedMileage = await _fuelService.CalculateMileageAsync(id.Value);
            ViewBag.Title = $"Fuel Log #{log.FuelLogId} Details";

            return View(log);
        }

        // ==========================================
        // GET: /Fuel/Create
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create(int? vehicleId, int? tripId)
        {
            ViewBag.Title = "Record Fuel Purchase Receipt";

            decimal prefilledOdometer = 0;
            if (vehicleId.HasValue)
            {
                var veh = await _context.Vehicles.FindAsync(vehicleId.Value);
                if (veh != null) prefilledOdometer = veh.CurrentOdometer;
            }

            var model = new CreateFuelLogViewModel
            {
                VehicleId = vehicleId ?? 0,
                TripId = tripId,
                FuelDate = DateTime.UtcNow,
                OdometerReading = prefilledOdometer,
                PricePerLitre = 1.65m // baseline default market estimation
            };

            await PopulateDropdownsAsync(model);
            return View(model);
        }

        // ==========================================
        // POST: /Fuel/Create
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateFuelLogViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Record Fuel Purchase Receipt";
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            var fuelLog = new FuelLog
            {
                VehicleId = model.VehicleId,
                TripId = model.TripId,
                FuelQuantity = model.FuelQuantity,
                PricePerLitre = model.PricePerLitre,
                FuelDate = model.FuelDate,
                FuelStation = model.FuelStation.Trim(),
                OdometerReading = model.OdometerReading
            };

            var (success, message) = await _fuelService.CreateFuelLogAsync(fuelLog);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                ViewBag.Title = "Record Fuel Purchase Receipt";
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            TempData["Success"] = message;

            if (User.IsInRole(DbSeeder.RoleDriver))
            {
                return RedirectToAction("MyTrips", "Trips");
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: /Fuel/Edit/5
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();

            var log = await _fuelService.GetByIdAsync(id.Value);
            if (log == null) return NotFound();

            var model = new CreateFuelLogViewModel
            {
                FuelLogId = log.FuelLogId,
                VehicleId = log.VehicleId,
                TripId = log.TripId,
                FuelQuantity = log.FuelQuantity,
                PricePerLitre = log.PricePerLitre,
                TotalCost = log.TotalCost,
                FuelDate = log.FuelDate,
                FuelStation = log.FuelStation,
                OdometerReading = log.OdometerReading
            };

            await PopulateDropdownsAsync(model);
            ViewBag.Title = $"Edit Fuel Log #{log.FuelLogId}";
            return View(model);
        }

        // ==========================================
        // POST: /Fuel/Edit/5
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateFuelLogViewModel model)
        {
            if (id != model.FuelLogId) return BadRequest();

            if (!ModelState.IsValid)
            {
                ViewBag.Title = $"Edit Fuel Log #{model.FuelLogId}";
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            var fuelLog = new FuelLog
            {
                FuelLogId = model.FuelLogId,
                VehicleId = model.VehicleId,
                TripId = model.TripId,
                FuelQuantity = model.FuelQuantity,
                PricePerLitre = model.PricePerLitre,
                FuelDate = model.FuelDate,
                FuelStation = model.FuelStation.Trim(),
                OdometerReading = model.OdometerReading
            };

            var (success, message) = await _fuelService.UpdateFuelLogAsync(fuelLog);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                ViewBag.Title = $"Edit Fuel Log #{model.FuelLogId}";
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: /Fuel/Delete/5
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();

            var log = await _fuelService.GetByIdAsync(id.Value);
            if (log == null) return NotFound();

            ViewBag.Title = $"Delete Fuel Log #{log.FuelLogId}";
            return View(log);
        }

        // ==========================================
        // POST: /Fuel/Delete/5
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (success, message) = await _fuelService.DeleteFuelLogAsync(id);
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
        // GET: /Fuel/MonthlySummary
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public async Task<IActionResult> MonthlySummary(int? year)
        {
            int selectedYear = year ?? DateTime.UtcNow.Year;
            var summaries = await _fuelService.GetMonthlyFuelExpensesAsync(selectedYear);

            ViewBag.SelectedYear = selectedYear;
            ViewBag.Title = $"Monthly Fuel Expenditure Summary ({selectedYear})";

            return View(summaries);
        }

        private async Task PopulateDropdownsAsync(CreateFuelLogViewModel model)
        {
            model.AvailableVehicles = await _context.Vehicles
                .OrderBy(v => v.VehicleName)
                .Select(v => new SelectListItem
                {
                    Value = v.VehicleId.ToString(),
                    Text = $"{v.VehicleName} ({v.RegistrationNumber}) - Odo: {v.CurrentOdometer:N0} km"
                })
                .ToListAsync();

            model.AvailableTrips = await _context.Trips
                .Where(t => model.VehicleId == 0 || t.VehicleId == model.VehicleId)
                .OrderByDescending(t => t.StartDate)
                .Take(20)
                .Select(t => new SelectListItem
                {
                    Value = t.TripId.ToString(),
                    Text = $"Trip #{t.TripId}: {t.Source} -> {t.Destination} ({t.StartDate:d})"
                })
                .ToListAsync();
        }
    }
}
