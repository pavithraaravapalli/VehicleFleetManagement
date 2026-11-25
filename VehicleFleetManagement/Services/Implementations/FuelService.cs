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
    /// Implements fuel receipt logging, cost calculation (Total = Qty * Price),
    /// odometer validation, mileage efficiency estimation (km/L), and monthly summaries.
    /// </summary>
    public class FuelService : IFuelService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FuelService> _logger;

        public FuelService(ApplicationDbContext context, ILogger<FuelService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<FuelLog>> GetAllFuelLogsAsync(int? vehicleId, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
        {
            var query = BuildFilteredQuery(vehicleId, fromDate, toDate);

            return await query
                .OrderByDescending(f => f.FuelDate)
                .ThenByDescending(f => f.FuelLogId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(int? vehicleId, DateTime? fromDate, DateTime? toDate)
        {
            var query = BuildFilteredQuery(vehicleId, fromDate, toDate);
            return await query.CountAsync();
        }

        public async Task<FuelLog?> GetByIdAsync(int id)
        {
            return await _context.FuelLogs
                .Include(f => f.Vehicle)
                .Include(f => f.Trip)
                .FirstOrDefaultAsync(f => f.FuelLogId == id);
        }

        public async Task<(bool Success, string Message)> CreateFuelLogAsync(FuelLog fuelLog)
        {
            try
            {
                if (fuelLog.FuelQuantity <= 0)
                {
                    return (false, "Fuel quantity must be greater than zero.");
                }

                if (fuelLog.PricePerLitre <= 0)
                {
                    return (false, "Price per litre must be greater than zero.");
                }

                if (fuelLog.OdometerReading < 0)
                {
                    return (false, "Odometer reading cannot be negative.");
                }

                var vehicle = await _context.Vehicles.FindAsync(fuelLog.VehicleId);
                if (vehicle == null)
                {
                    return (false, "Selected vehicle does not exist.");
                }

                // Check previous fuel log reading for odometer consistency
                var previousLog = await _context.FuelLogs
                    .Where(f => f.VehicleId == fuelLog.VehicleId && f.FuelDate <= fuelLog.FuelDate)
                    .OrderByDescending(f => f.FuelDate)
                    .ThenByDescending(f => f.FuelLogId)
                    .FirstOrDefaultAsync();

                if (previousLog != null && fuelLog.OdometerReading < previousLog.OdometerReading)
                {
                    return (false, $"Odometer reading ({fuelLog.OdometerReading:N0} km) cannot be lower than the vehicle's prior logged fuel refill ({previousLog.OdometerReading:N0} km on {previousLog.FuelDate:d}).");
                }

                // Automatic Total Cost formula
                fuelLog.TotalCost = Math.Round(fuelLog.FuelQuantity * fuelLog.PricePerLitre, 2);

                _context.FuelLogs.Add(fuelLog);

                // If refill odometer is higher than vehicle's recorded odometer, synchronize vehicle
                if (fuelLog.OdometerReading > vehicle.CurrentOdometer)
                {
                    vehicle.CurrentOdometer = fuelLog.OdometerReading;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("FuelLog #{Id} created for Vehicle {VehicleId}", fuelLog.FuelLogId, fuelLog.VehicleId);

                return (true, $"Fuel purchase of {fuelLog.FuelQuantity:N2} litres (${fuelLog.TotalCost:N2}) logged successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fuel log");
                return (false, "An error occurred while saving the fuel purchase log.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateFuelLogAsync(FuelLog fuelLog)
        {
            try
            {
                if (fuelLog.FuelQuantity <= 0 || fuelLog.PricePerLitre <= 0)
                {
                    return (false, "Fuel quantity and price per litre must be strictly positive.");
                }

                var existing = await _context.FuelLogs.FindAsync(fuelLog.FuelLogId);
                if (existing == null) return (false, "Fuel log not found.");

                existing.VehicleId = fuelLog.VehicleId;
                existing.TripId = fuelLog.TripId;
                existing.FuelQuantity = fuelLog.FuelQuantity;
                existing.PricePerLitre = fuelLog.PricePerLitre;
                existing.TotalCost = Math.Round(fuelLog.FuelQuantity * fuelLog.PricePerLitre, 2);
                existing.FuelDate = fuelLog.FuelDate;
                existing.FuelStation = fuelLog.FuelStation;
                existing.OdometerReading = fuelLog.OdometerReading;

                await _context.SaveChangesAsync();
                _logger.LogInformation("FuelLog #{Id} updated", fuelLog.FuelLogId);

                return (true, "Fuel log updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fuel log #{Id}", fuelLog.FuelLogId);
                return (false, "An error occurred while updating the fuel record.");
            }
        }

        public async Task<(bool Success, string Message)> DeleteFuelLogAsync(int id)
        {
            try
            {
                var log = await _context.FuelLogs.FindAsync(id);
                if (log == null) return (false, "Fuel log not found.");

                _context.FuelLogs.Remove(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation("FuelLog #{Id} deleted", id);
                return (true, "Fuel record deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fuel log #{Id}", id);
                return (false, "An error occurred while deleting the fuel record.");
            }
        }

        public async Task<decimal> GetTotalFuelExpensesAsync(int? vehicleId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = BuildFilteredQuery(vehicleId, fromDate, toDate);
            return await query.SumAsync(f => (decimal?)f.TotalCost) ?? 0m;
        }

        public async Task<decimal?> CalculateMileageAsync(int fuelLogId)
        {
            var currentLog = await _context.FuelLogs
                .Include(f => f.Trip)
                .FirstOrDefaultAsync(f => f.FuelLogId == fuelLogId);

            if (currentLog == null || currentLog.FuelQuantity <= 0) return null;

            // Strategy A: Compare with previous fuel log odometer
            var previousLog = await _context.FuelLogs
                .Where(f => f.VehicleId == currentLog.VehicleId &&
                            (f.FuelDate < currentLog.FuelDate || (f.FuelDate == currentLog.FuelDate && f.FuelLogId < currentLog.FuelLogId)))
                .OrderByDescending(f => f.FuelDate)
                .ThenByDescending(f => f.FuelLogId)
                .FirstOrDefaultAsync();

            if (previousLog != null && currentLog.OdometerReading > previousLog.OdometerReading)
            {
                var distance = currentLog.OdometerReading - previousLog.OdometerReading;
                return Math.Round(distance / currentLog.FuelQuantity, 2);
            }

            // Strategy B: If linked to a completed trip with recorded distance
            if (currentLog.Trip != null && currentLog.Trip.Distance > 0)
            {
                return Math.Round(currentLog.Trip.Distance / currentLog.FuelQuantity, 2);
            }

            return null; // Insufficient previous baseline to compute valid mileage
        }

        public async Task<IEnumerable<MonthlyFuelExpenseSummary>> GetMonthlyFuelExpensesAsync(int year)
        {
            var logs = await _context.FuelLogs
                .Where(f => f.FuelDate.Year == year)
                .AsNoTracking()
                .ToListAsync();

            var summaries = logs
                .GroupBy(f => f.FuelDate.Month)
                .Select(g => new MonthlyFuelExpenseSummary
                {
                    Year = year,
                    Month = g.Key,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key),
                    TotalCost = g.Sum(x => x.TotalCost),
                    TotalQuantity = g.Sum(x => x.FuelQuantity),
                    RefillCount = g.Count()
                })
                .OrderBy(s => s.Month)
                .ToList();

            return summaries;
        }

        private IQueryable<FuelLog> BuildFilteredQuery(int? vehicleId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.FuelLogs
                .Include(f => f.Vehicle)
                .Include(f => f.Trip)
                .AsNoTracking()
                .AsQueryable();

            if (vehicleId.HasValue && vehicleId.Value > 0)
            {
                query = query.Where(f => f.VehicleId == vehicleId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(f => f.FuelDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(f => f.FuelDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            return query;
        }
    }
}
