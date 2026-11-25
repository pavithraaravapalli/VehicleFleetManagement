using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VehicleFleetManagement.Data;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.Services.Interfaces;

namespace VehicleFleetManagement.Services.Implementations
{
    /// <summary>
    /// Implements vehicle asset management, business validation, uniqueness checks,
    /// and historical relationship tracking.
    /// </summary>
    public class VehicleService : IVehicleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VehicleService> _logger;

        public VehicleService(ApplicationDbContext context, ILogger<VehicleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Vehicle>> GetAllVehiclesAsync(string? searchTerm, string? vehicleType, string? status, int page = 1, int pageSize = 10)
        {
            var query = BuildFilteredQuery(searchTerm, vehicleType, status);

            return await query
                .OrderByDescending(v => v.VehicleId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalVehicleCountAsync(string? searchTerm, string? vehicleType, string? status)
        {
            var query = BuildFilteredQuery(searchTerm, vehicleType, status);
            return await query.CountAsync();
        }

        public async Task<Vehicle?> GetVehicleByIdAsync(int id)
        {
            return await _context.Vehicles.FindAsync(id);
        }

        public async Task<Vehicle?> GetVehicleWithHistoryAsync(int id)
        {
            return await _context.Vehicles
                .Include(v => v.Trips.OrderByDescending(t => t.StartDate))
                    .ThenInclude(t => t.Driver)
                .Include(v => v.Maintenances.OrderByDescending(m => m.ServiceDate))
                .Include(v => v.FuelLogs.OrderByDescending(f => f.FuelDate))
                .FirstOrDefaultAsync(v => v.VehicleId == id);
        }

        public async Task<(bool Success, string Message)> CreateVehicleAsync(Vehicle vehicle)
        {
            try
            {
                vehicle.RegistrationNumber = vehicle.RegistrationNumber.Trim().ToUpperInvariant();

                // Unique registration validation
                var exists = await _context.Vehicles.AnyAsync(v => v.RegistrationNumber == vehicle.RegistrationNumber);
                if (exists)
                {
                    return (false, $"A vehicle with registration number '{vehicle.RegistrationNumber}' is already registered in the system.");
                }

                vehicle.CreatedAt = DateTime.UtcNow;
                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Vehicle {RegNumber} created with ID {VehicleId}", vehicle.RegistrationNumber, vehicle.VehicleId);
                return (true, $"Vehicle '{vehicle.VehicleName}' ({vehicle.RegistrationNumber}) was added successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vehicle {RegNumber}", vehicle.RegistrationNumber);
                return (false, "An error occurred while saving the vehicle. Please verify input values.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateVehicleAsync(Vehicle vehicle)
        {
            try
            {
                vehicle.RegistrationNumber = vehicle.RegistrationNumber.Trim().ToUpperInvariant();

                // Check registration number uniqueness excluding current vehicle
                var conflict = await _context.Vehicles.AnyAsync(v =>
                    v.RegistrationNumber == vehicle.RegistrationNumber &&
                    v.VehicleId != vehicle.VehicleId);

                if (conflict)
                {
                    return (false, $"Registration number '{vehicle.RegistrationNumber}' is already assigned to another vehicle.");
                }

                var existing = await _context.Vehicles.FindAsync(vehicle.VehicleId);
                if (existing == null)
                {
                    return (false, "The requested vehicle record could not be found.");
                }

                // Update mutable fields
                existing.RegistrationNumber = vehicle.RegistrationNumber;
                existing.VehicleName = vehicle.VehicleName;
                existing.Model = vehicle.Model;
                existing.VehicleType = vehicle.VehicleType;
                existing.FuelType = vehicle.FuelType;
                existing.ManufacturingYear = vehicle.ManufacturingYear;
                existing.SeatingCapacity = vehicle.SeatingCapacity;
                existing.CurrentOdometer = vehicle.CurrentOdometer;
                existing.RegistrationDate = vehicle.RegistrationDate;
                existing.InsuranceExpiryDate = vehicle.InsuranceExpiryDate;
                existing.Status = vehicle.Status;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Vehicle ID {VehicleId} updated successfully", vehicle.VehicleId);
                return (true, $"Vehicle '{vehicle.RegistrationNumber}' updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vehicle ID {VehicleId}", vehicle.VehicleId);
                return (false, "An error occurred while updating the vehicle record.");
            }
        }

        public async Task<(bool Success, string Message)> DeleteVehicleAsync(int id)
        {
            try
            {
                var vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                {
                    return (false, "Vehicle record not found.");
                }

                // Business check: If vehicle is associated with historical trips, restrict deletion to preserve audit trail
                var hasTrips = await _context.Trips.AnyAsync(t => t.VehicleId == id);
                if (hasTrips)
                {
                    return (false, "Cannot permanently delete this vehicle because it has historical or active trip records. Please set its status to 'Inactive' instead.");
                }

                var hasFuelLogs = await _context.FuelLogs.AnyAsync(f => f.VehicleId == id);
                if (hasFuelLogs)
                {
                    return (false, "Cannot delete vehicle because it has logged fuel expenses. Mark the vehicle as 'Inactive' to preserve expense records.");
                }

                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Vehicle ID {VehicleId} deleted", id);
                return (true, $"Vehicle '{vehicle.RegistrationNumber}' was permanently deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vehicle ID {VehicleId}", id);
                return (false, "Failed to delete vehicle due to database relational constraints.");
            }
        }

        public async Task<IEnumerable<string>> GetDistinctVehicleTypesAsync()
        {
            return await _context.Vehicles
                .Select(v => v.VehicleType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }

        public async Task<IEnumerable<Vehicle>> GetAvailableVehiclesAsync()
        {
            return await _context.Vehicles
                .Where(v => v.Status == VehicleStatus.Available)
                .OrderBy(v => v.VehicleName)
                .ToListAsync();
        }

        private IQueryable<Vehicle> BuildFilteredQuery(string? searchTerm, string? vehicleType, string? status)
        {
            var query = _context.Vehicles.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(v =>
                    v.RegistrationNumber.ToLower().Contains(term) ||
                    v.VehicleName.ToLower().Contains(term) ||
                    v.Model.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(vehicleType) && vehicleType != "All")
            {
                query = query.Where(v => v.VehicleType == vehicleType);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(v => v.Status == status);
            }

            return query;
        }
    }
}
