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
    /// Implements trip dispatching, conflict detection, transaction-safe lifecycle transitions,
    /// and odometer/distance auditing.
    /// </summary>
    public class TripService : ITripService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TripService> _logger;

        public TripService(ApplicationDbContext context, ILogger<TripService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Trip>> GetAllTripsAsync(string? searchTerm, string? status, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
        {
            var query = BuildFilteredQuery(searchTerm, status, fromDate, toDate);

            return await query
                .OrderByDescending(t => t.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalTripsCountAsync(string? searchTerm, string? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = BuildFilteredQuery(searchTerm, status, fromDate, toDate);
            return await query.CountAsync();
        }

        public async Task<IEnumerable<Trip>> GetDriverTripsAsync(int driverId, string? status = null)
        {
            var query = _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Driver)
                .Where(t => t.DriverId == driverId);

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(t => t.TripStatus == status);
            }

            return await query
                .OrderByDescending(t => t.StartDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Trip?> GetTripByIdAsync(int id)
        {
            return await _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Driver)
                .Include(t => t.FuelLogs)
                .FirstOrDefaultAsync(t => t.TripId == id);
        }

        public async Task<(bool Success, string Message)> CreateTripAsync(Trip trip)
        {
            try
            {
                if (trip.ExpectedEndDate <= trip.StartDate)
                {
                    return (false, "Expected arrival date and time must be later than the scheduled departure time.");
                }

                // 1. Check vehicle availability and conflicts
                var (vehicleConflict, vehicleMsg) = await CheckVehicleAvailabilityAsync(trip.VehicleId, trip.StartDate, trip.ExpectedEndDate);
                if (vehicleConflict)
                {
                    return (false, vehicleMsg);
                }

                // 2. Check driver availability and conflicts
                var (driverConflict, driverMsg) = await CheckDriverAvailabilityAsync(trip.DriverId, trip.StartDate, trip.ExpectedEndDate);
                if (driverConflict)
                {
                    return (false, driverMsg);
                }

                // Fetch vehicle to synchronize initial odometer
                var vehicle = await _context.Vehicles.FindAsync(trip.VehicleId);
                if (vehicle != null && trip.StartOdometer < vehicle.CurrentOdometer)
                {
                    trip.StartOdometer = vehicle.CurrentOdometer;
                }

                trip.TripStatus = TripStatusEnum.Scheduled;
                trip.CreatedAt = DateTime.UtcNow;

                _context.Trips.Add(trip);

                // Proactively create an automated Notification if the driver has a registered user account
                var driver = await _context.Drivers.FindAsync(trip.DriverId);
                if (driver != null && !string.IsNullOrEmpty(driver.UserId))
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = driver.UserId,
                        Title = "New Trip Assignment",
                        Message = $"You have been assigned to journey #{trip.TripId} from {trip.Source} to {trip.Destination}. Scheduled departure: {trip.StartDate:g}.",
                        Type = NotificationType.TripAssigned,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    });
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Trip #{TripId} scheduled for Vehicle {VehicleId} and Driver {DriverId}", trip.TripId, trip.VehicleId, trip.DriverId);

                return (true, $"Trip #{trip.TripId} successfully scheduled from {trip.Source} to {trip.Destination}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trip");
                return (false, "An error occurred while creating the trip record.");
            }
        }

        public async Task<(bool Success, string Message)> UpdateTripAsync(Trip trip)
        {
            try
            {
                var existing = await _context.Trips.FindAsync(trip.TripId);
                if (existing == null) return (false, "Trip not found.");

                existing.Source = trip.Source;
                existing.Destination = trip.Destination;
                existing.StartDate = trip.StartDate;
                existing.ExpectedEndDate = trip.ExpectedEndDate;
                existing.StartOdometer = trip.StartOdometer;
                existing.Purpose = trip.Purpose;
                existing.TripStatus = trip.TripStatus;

                await _context.SaveChangesAsync();
                return (true, $"Trip #{trip.TripId} updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trip #{TripId}", trip.TripId);
                return (false, "An error occurred while updating the trip record.");
            }
        }

        public async Task<(bool Success, string Message)> DeleteTripAsync(int id)
        {
            try
            {
                var trip = await _context.Trips.FindAsync(id);
                if (trip == null) return (false, "Trip not found.");

                if (trip.TripStatus == TripStatusEnum.InProgress)
                {
                    return (false, "Cannot delete an active trip that is currently In Progress.");
                }

                _context.Trips.Remove(trip);
                await _context.SaveChangesAsync();
                return (true, $"Trip #{id} deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting trip #{Id}", id);
                return (false, "An error occurred while deleting the trip record.");
            }
        }

        public async Task<(bool Success, string Message)> StartTripAsync(int tripId, decimal startOdometer, string? currentUserId = null, bool isAdmin = false)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trip = await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver)
                    .FirstOrDefaultAsync(t => t.TripId == tripId);

                if (trip == null)
                {
                    return (false, "Trip not found.");
                }

                // Authorization check: only Admin or assigned driver may start the trip
                if (!isAdmin && trip.Driver?.UserId != currentUserId)
                {
                    return (false, "Unauthorized: You are not assigned as the operating driver for this journey.");
                }

                if (trip.TripStatus != TripStatusEnum.Scheduled)
                {
                    return (false, $"Cannot start trip. Current trip status is '{trip.TripStatus}' (only 'Scheduled' trips can be started).");
                }

                if (trip.Vehicle == null || trip.Driver == null)
                {
                    return (false, "Vehicle or driver details missing.");
                }

                if (trip.Vehicle.Status == VehicleStatus.UnderMaintenance)
                {
                    return (false, "Cannot start trip: The assigned vehicle is currently marked 'Under Maintenance'.");
                }

                if (startOdometer < trip.Vehicle.CurrentOdometer)
                {
                    return (false, $"Start odometer reading ({startOdometer} km) cannot be less than the vehicle's current recorded odometer ({trip.Vehicle.CurrentOdometer} km).");
                }

                // Update Trip status
                trip.StartOdometer = startOdometer;
                trip.ActualStartDate = DateTime.UtcNow;
trip.TripStatus = TripStatusEnum.InProgress;

                // Update Vehicle status & odometer
                trip.Vehicle.Status = VehicleStatus.Assigned;
                trip.Vehicle.CurrentOdometer = startOdometer;

                // Update Driver status
                trip.Driver.AvailabilityStatus = DriverStatus.OnTrip;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Trip #{TripId} started by user {User}", tripId, currentUserId ?? "Admin");
                return (true, $"Trip #{tripId} is now In Progress. Vehicle and driver marked as dispatched.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error starting trip #{TripId}", tripId);
                return (false, "Failed to start the trip due to a database transaction error.");
            }
        }

        public async Task<(bool Success, string Message)> CompleteTripAsync(int tripId, decimal endOdometer, DateTime? actualEndDate = null, string? currentUserId = null, bool isAdmin = false, string? tripNotes = null)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trip = await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver)
                    .FirstOrDefaultAsync(t => t.TripId == tripId);

                if (trip == null)
                {
                    return (false, "Trip not found.");
                }

                // Driver authorization verification
                if (!isAdmin && trip.Driver?.UserId != currentUserId)
                {
                    return (false, "Unauthorized: You cannot complete a trip that is assigned to another driver.");
                }

                if (trip.TripStatus != TripStatusEnum.InProgress)
                {
                    return (false, $"Cannot complete trip. Trip status is '{trip.TripStatus}' (must be 'InProgress' to complete).");
                }

                if (endOdometer < trip.StartOdometer)
                {
                    return (false, $"Final odometer reading ({endOdometer} km) cannot be less than the starting odometer reading ({trip.StartOdometer} km).");
                }

                // Distance calculation
                var distanceTravelled = endOdometer - trip.StartOdometer;

                // Update Trip
                trip.EndOdometer = endOdometer;
                trip.Distance = distanceTravelled;
                trip.ActualEndDate = actualEndDate ?? DateTime.UtcNow;
                trip.TripNotes = tripNotes;
trip.TripStatus = TripStatusEnum.Completed;

                // Release Vehicle
                if (trip.Vehicle != null)
                {
                    trip.Vehicle.CurrentOdometer = endOdometer;
                    trip.Vehicle.Status = VehicleStatus.Available;
                }

                // Release Driver
                if (trip.Driver != null)
                {
                    trip.Driver.AvailabilityStatus = DriverStatus.Available;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Trip #{TripId} completed with distance {Dist} km", tripId, distanceTravelled);
                return (true, $"Trip #{tripId} completed successfully. Distance covered: {distanceTravelled:N2} km. Vehicle and driver released to Available.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error completing trip #{TripId}", tripId);
                return (false, "Failed to complete trip due to a database transaction error.");
            }
        }

        public async Task<(bool Success, string Message)> CancelTripAsync(int tripId, string? reason = null, string? currentUserId = null, bool isAdmin = false)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trip = await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver)
                    .FirstOrDefaultAsync(t => t.TripId == tripId);

                if (trip == null)
                {
                    return (false, "Trip not found.");
                }

                if (trip.TripStatus == TripStatusEnum.Completed)
                {
                    return (false, "Completed trips cannot be cancelled.");
                }

                if (trip.TripStatus == TripStatusEnum.Cancelled)
                {
                    return (false, "This trip has already been cancelled.");
                }

                // If trip was InProgress, release the vehicle and driver back to Available
                if (trip.TripStatus == TripStatusEnum.InProgress)
                {
                    if (trip.Vehicle != null) trip.Vehicle.Status = VehicleStatus.Available;
                    if (trip.Driver != null) trip.Driver.AvailabilityStatus = DriverStatus.Available;
                }

                trip.TripStatus = TripStatusEnum.Cancelled;
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    trip.Purpose += $" [Cancelled: {reason}]";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Trip #{TripId} cancelled.", tripId);
                return (true, $"Trip #{tripId} has been cancelled.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling trip #{TripId}", tripId);
                return (false, "Failed to cancel trip due to a database error.");
            }
        }

        public async Task<(bool Conflict, string Message)> CheckVehicleAvailabilityAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeTripId = null)
        {
            var vehicle = await _context.Vehicles.FindAsync(vehicleId);
            if (vehicle == null)
            {
                return (true, "Selected vehicle does not exist.");
            }

            if (vehicle.Status == VehicleStatus.UnderMaintenance)
            {
                return (true, $"Vehicle '{vehicle.RegistrationNumber}' is currently under maintenance.");
            }

            if (vehicle.Status == VehicleStatus.Inactive)
            {
                return (true, $"Vehicle '{vehicle.RegistrationNumber}' is currently marked inactive.");
            }

            // Check for overlapping active or scheduled trips
            var overlappingTrip = await _context.Trips
                .Where(t => t.VehicleId == vehicleId &&
                            t.TripStatus != TripStatusEnum.Completed &&
                            t.TripStatus != TripStatusEnum.Cancelled &&
                            (excludeTripId == null || t.TripId != excludeTripId.Value) &&
                            t.StartDate < endDate && t.ExpectedEndDate > startDate)
                .FirstOrDefaultAsync();

            if (overlappingTrip != null)
            {
                return (true, $"Vehicle '{vehicle.RegistrationNumber}' is already booked for Trip #{overlappingTrip.TripId} between {overlappingTrip.StartDate:g} and {overlappingTrip.ExpectedEndDate:g}.");
            }

            return (false, string.Empty);
        }

        public async Task<(bool Conflict, string Message)> CheckDriverAvailabilityAsync(int driverId, DateTime startDate, DateTime endDate, int? excludeTripId = null)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
            {
                return (true, "Selected driver does not exist.");
            }

            if (driver.AvailabilityStatus == DriverStatus.Inactive)
            {
                return (true, $"Driver '{driver.FullName}' is inactive or deactivated.");
            }

            if (driver.LicenseExpiryDate < startDate)
            {
                return (true, $"Driver '{driver.FullName}' has a driving license that will expire ({driver.LicenseExpiryDate:d}) before or during the scheduled trip.");
            }

            // Check for overlapping active or scheduled trips
            var overlappingTrip = await _context.Trips
                .Where(t => t.DriverId == driverId &&
                            t.TripStatus != TripStatusEnum.Completed &&
                            t.TripStatus != TripStatusEnum.Cancelled &&
                            (excludeTripId == null || t.TripId != excludeTripId.Value) &&
                            t.StartDate < endDate && t.ExpectedEndDate > startDate)
                .FirstOrDefaultAsync();

            if (overlappingTrip != null)
            {
                return (true, $"Driver '{driver.FullName}' is already assigned to Trip #{overlappingTrip.TripId} ({overlappingTrip.StartDate:g} to {overlappingTrip.ExpectedEndDate:g}).");
            }

            return (false, string.Empty);
        }

        private IQueryable<Trip> BuildFilteredQuery(string? searchTerm, string? status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Trips
                .Include(t => t.Vehicle)
                .Include(t => t.Driver)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(t =>
                    t.Source.ToLower().Contains(term) ||
                    t.Destination.ToLower().Contains(term) ||
                    t.Purpose.ToLower().Contains(term) ||
                    (t.Vehicle != null && (t.Vehicle.RegistrationNumber.ToLower().Contains(term) || t.Vehicle.VehicleName.ToLower().Contains(term))) ||
                    (t.Driver != null && t.Driver.FullName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(t => t.TripStatus == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.StartDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(t => t.StartDate <= toDate.Value.AddDays(1).AddTicks(-1));
            }

            return query;
        }
    }
}

