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
    /// Implements driver credential tracking, license compliance audits,
    /// safe deactivation, and trip assignment history.
    /// </summary>
    public class DriverService : IDriverService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DriverService> _logger;

        public DriverService(ApplicationDbContext context, ILogger<DriverService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Driver>> GetAllDriversAsync(string? searchTerm, string? availabilityStatus, int page = 1, int pageSize = 10)
        {
            var query = BuildFilteredQuery(searchTerm, availabilityStatus);

            return await query
                .OrderByDescending(d => d.DriverId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalDriverCountAsync(string? searchTerm, string? availabilityStatus)
        {
            var query = BuildFilteredQuery(searchTerm, availabilityStatus);
            return await query.CountAsync();
        }

        public async Task<Driver?> GetDriverByIdAsync(int id)
        {
            return await _context.Drivers.FindAsync(id);
        }

        public async Task<Driver?> GetDriverWithHistoryAsync(int id)
        {
            return await _context.Drivers
                .Include(d => d.User)
                .Include(d => d.Trips.OrderByDescending(t => t.StartDate))
                    .ThenInclude(t => t.Vehicle)
                .FirstOrDefaultAsync(d => d.DriverId == id);
        }

        public async Task<(bool Success, string Message)> CreateDriverAsync(Driver driver)
        {
            try
            {
                driver.LicenseNumber = driver.LicenseNumber.Trim().ToUpperInvariant();

                // Unique license number validation
                var exists = await _context.Drivers.AnyAsync(d => d.LicenseNumber == driver.LicenseNumber);
                if (exists)
                {
                    return (false, $"A driver with license number '{driver.LicenseNumber}' is already registered.");
                }

                _context.Drivers.Add(driver);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Driver {FullName} registered with ID {DriverId}", driver.FullName, driver.DriverId);
                return (true, $"Driver '{driver.FullName}' registered successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering driver {FullName}", driver.FullName);
                return (false, "An error occurred while creating the driver record. Please verify input fields.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateDriverAsync(Driver driver)
        {
            try
            {
                driver.LicenseNumber = driver.LicenseNumber.Trim().ToUpperInvariant();

                // Uniqueness check excluding current driver
                var conflict = await _context.Drivers.AnyAsync(d =>
                    d.LicenseNumber == driver.LicenseNumber &&
                    d.DriverId != driver.DriverId);

                if (conflict)
                {
                    return (false, $"License number '{driver.LicenseNumber}' is already associated with another driver.");
                }

                var existing = await _context.Drivers.FindAsync(driver.DriverId);
                if (existing == null)
                {
                    return (false, "Driver record not found.");
                }

                existing.FullName = driver.FullName;
                existing.Email = driver.Email;
                existing.PhoneNumber = driver.PhoneNumber;
                existing.LicenseNumber = driver.LicenseNumber;
                existing.LicenseExpiryDate = driver.LicenseExpiryDate;
                existing.Address = driver.Address;
                existing.JoiningDate = driver.JoiningDate;
                existing.AvailabilityStatus = driver.AvailabilityStatus;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Driver ID {DriverId} updated successfully", driver.DriverId);
                return (true, $"Driver '{driver.FullName}' updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating driver ID {DriverId}", driver.DriverId);
                return (false, "An error occurred while updating the driver profile.");
            }
        }

        public async Task<(bool Success, string Message)> DeleteOrDeactivateDriverAsync(int id)
        {
            try
            {
                var driver = await _context.Drivers.FindAsync(id);
                if (driver == null)
                {
                    return (false, "Driver record not found.");
                }

                // If driver has trip history, deactivate safely to prevent orphaned trip records
                var hasTrips = await _context.Trips.AnyAsync(t => t.DriverId == id);
                if (hasTrips)
                {
                    driver.AvailabilityStatus = DriverStatus.Inactive;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Driver ID {DriverId} deactivated due to existing trip records", id);
                    return (true, $"Driver '{driver.FullName}' has historical trip logs. For relational integrity, the driver was deactivated ('Inactive') instead of deleted.");
                }

                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Driver ID {DriverId} permanently deleted", id);
                return (true, $"Driver '{driver.FullName}' was permanently deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting/deactivating driver ID {DriverId}", id);
                return (false, "An error occurred while processing driver removal.");
            }
        }

        public async Task<IEnumerable<Driver>> GetAvailableDriversAsync()
        {
            return await _context.Drivers
                .Where(d => d.AvailabilityStatus == DriverStatus.Available)
                .OrderBy(d => d.FullName)
                .ToListAsync();
        }

        private IQueryable<Driver> BuildFilteredQuery(string? searchTerm, string? availabilityStatus)
        {
            var query = _context.Drivers.Include(d => d.Trips).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(d =>
                    d.FullName.ToLower().Contains(term) ||
                    d.LicenseNumber.ToLower().Contains(term) ||
                    d.Email.ToLower().Contains(term) ||
                    d.PhoneNumber.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(availabilityStatus) && availabilityStatus != "All")
            {
                query = query.Where(d => d.AvailabilityStatus == availabilityStatus);
            }

            return query;
        }
    }
}
