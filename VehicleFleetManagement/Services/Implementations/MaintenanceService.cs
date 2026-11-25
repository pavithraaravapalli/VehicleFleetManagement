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
    /// Implements vehicle preventive maintenance scheduling, cost tracking,
    /// 7-day upcoming reminder detection, and vehicle availability status updates.
    /// </summary>
    public class MaintenanceService : IMaintenanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MaintenanceService> _logger;

        public MaintenanceService(ApplicationDbContext context, ILogger<MaintenanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Maintenance>> GetAllMaintenanceAsync(string? searchTerm, string? status, int? vehicleId, int page = 1, int pageSize = 10)
        {
            var query = BuildFilteredQuery(searchTerm, status, vehicleId);

            return await query
                .OrderByDescending(m => m.ServiceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? searchTerm, string? status, int? vehicleId)
        {
            var query = BuildFilteredQuery(searchTerm, status, vehicleId);
            return await query.CountAsync();
        }

        public async Task<Maintenance?> GetByIdAsync(int id)
        {
            return await _context.Maintenances
                .Include(m => m.Vehicle)
                .FirstOrDefaultAsync(m => m.MaintenanceId == id);
        }

        public async Task<IEnumerable<Maintenance>> GetUpcomingMaintenanceAlertsAsync(int daysThreshold = 7)
        {
            var today = DateTime.UtcNow.Date;
            var thresholdDate = today.AddDays(daysThreshold).AddDays(1).AddTicks(-1);

            return await _context.Maintenances
                .Include(m => m.Vehicle)
                .Where(m => (m.Status != MaintenanceStatus.Completed &&
                             ((m.ServiceDate >= today && m.ServiceDate <= thresholdDate) ||
                              (m.NextServiceDate >= today && m.NextServiceDate <= thresholdDate)))
                            || (m.Status == MaintenanceStatus.Completed &&
                                m.NextServiceDate >= today && m.NextServiceDate <= thresholdDate))
                .OrderBy(m => m.ServiceDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Maintenance>> GetOverdueMaintenanceAsync()
        {
            var today = DateTime.UtcNow.Date;

            return await _context.Maintenances
                .Include(m => m.Vehicle)
                .Where(m => (m.Status != MaintenanceStatus.Completed &&
                             (m.ServiceDate < today || m.NextServiceDate < today))
                            || (m.Status == MaintenanceStatus.Completed &&
                                m.NextServiceDate < today))
                .OrderBy(m => m.ServiceDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> CreateMaintenanceAsync(Maintenance maintenance)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (maintenance.Cost < 0)
                {
                    return (false, "Maintenance cost cannot be negative.");
                }

                if (maintenance.NextServiceDate < maintenance.ServiceDate)
                {
                    return (false, "Next service due date cannot be earlier than current service date.");
                }

                var vehicle = await _context.Vehicles.FindAsync(maintenance.VehicleId);
                if (vehicle == null)
                {
                    return (false, "Selected vehicle does not exist.");
                }

                // If maintenance is marked InProgress, flag vehicle as UnderMaintenance
                if (maintenance.Status == MaintenanceStatus.InProgress)
                {
                    vehicle.Status = VehicleStatus.UnderMaintenance;
                }

                _context.Maintenances.Add(maintenance);

                // Proactively create an automated alert if service is due within 7 days
                if (maintenance.NextServiceDate <= DateTime.UtcNow.AddDays(7) && maintenance.Status != MaintenanceStatus.Completed)
                {
                    // Find Admin user
                    var adminUser = await _context.Users.FirstOrDefaultAsync();
                    if (adminUser != null)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = adminUser.Id,
                            Title = "Maintenance Due Reminder",
                            Message = $"Vehicle '{vehicle.RegistrationNumber}' has a service due on {maintenance.NextServiceDate:d} ({maintenance.ServiceType}).",
                            Type = NotificationType.MaintenanceDue,
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Maintenance record #{Id} created for Vehicle {Reg}", maintenance.MaintenanceId, vehicle.RegistrationNumber);
                return (true, $"Maintenance record for '{vehicle.RegistrationNumber}' successfully scheduled.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating maintenance record");
                return (false, "An error occurred while saving the maintenance log.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateMaintenanceAsync(Maintenance maintenance)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (maintenance.Cost < 0) return (false, "Maintenance cost cannot be negative.");

                var existing = await _context.Maintenances
                    .Include(m => m.Vehicle)
                    .FirstOrDefaultAsync(m => m.MaintenanceId == maintenance.MaintenanceId);

                if (existing == null) return (false, "Maintenance record not found.");

                existing.ServiceType = maintenance.ServiceType;
                existing.Description = maintenance.Description;
                existing.ServiceDate = maintenance.ServiceDate;
                existing.NextServiceDate = maintenance.NextServiceDate;
                existing.Cost = maintenance.Cost;
                existing.ServiceCenter = maintenance.ServiceCenter;
                existing.Status = maintenance.Status;

                // Adjust vehicle status based on service completion
                if (existing.Vehicle != null)
                {
                    if (maintenance.Status == MaintenanceStatus.InProgress)
                    {
                        existing.Vehicle.Status = VehicleStatus.UnderMaintenance;
                    }
                    else if (maintenance.Status == MaintenanceStatus.Completed)
                    {
                        // Check if any other service is still InProgress for this vehicle
                        var otherOngoing = await _context.Maintenances.AnyAsync(m =>
                            m.VehicleId == existing.VehicleId &&
                            m.MaintenanceId != existing.MaintenanceId &&
                            m.Status == MaintenanceStatus.InProgress);

                        if (!otherOngoing && existing.Vehicle.Status == VehicleStatus.UnderMaintenance)
                        {
                            existing.Vehicle.Status = VehicleStatus.Available;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Maintenance #{Id} updated", maintenance.MaintenanceId);
                return (true, "Maintenance record updated successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating maintenance #{Id}", maintenance.MaintenanceId);
                return (false, "An error occurred while updating the maintenance record.");
            }
        }

        public async Task<(bool Success, string Message)> DeleteMaintenanceAsync(int id)
        {
            try
            {
                var maintenance = await _context.Maintenances.FindAsync(id);
                if (maintenance == null) return (false, "Maintenance record not found.");

                _context.Maintenances.Remove(maintenance);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Maintenance #{Id} deleted", id);
                return (true, "Maintenance record removed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting maintenance #{Id}", id);
                return (false, "An error occurred while deleting the maintenance entry.");
            }
        }

        public async Task<decimal> GetTotalMaintenanceCostAsync()
        {
            return await _context.Maintenances.SumAsync(m => (decimal?)m.Cost) ?? 0m;
        }

        private IQueryable<Maintenance> BuildFilteredQuery(string? searchTerm, string? status, int? vehicleId)
        {
            var query = _context.Maintenances
                .Include(m => m.Vehicle)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(m =>
                    m.ServiceType.ToLower().Contains(term) ||
                    m.ServiceCenter.ToLower().Contains(term) ||
                    m.Description.ToLower().Contains(term) ||
                    (m.Vehicle != null && (m.Vehicle.RegistrationNumber.ToLower().Contains(term) || m.Vehicle.VehicleName.ToLower().Contains(term))));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(m => m.Status == status);
            }

            if (vehicleId.HasValue && vehicleId.Value > 0)
            {
                query = query.Where(m => m.VehicleId == vehicleId.Value);
            }

            return query;
        }
    }
}
