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
    public class TripRequestService : ITripRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITripService _tripService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<TripRequestService> _logger;

        public TripRequestService(
            ApplicationDbContext context,
            ITripService tripService,
            INotificationService notificationService,
            ILogger<TripRequestService> logger)
        {
            _context = context;
            _tripService = tripService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IEnumerable<TripRequest>> GetUserTripRequestsAsync(string userId)
        {
            return await _context.TripRequests
                .Include(r => r.ApprovedTrip)
                    .ThenInclude(t => t!.Vehicle)
                .Include(r => r.ApprovedTrip)
                    .ThenInclude(t => t!.Driver)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<TripRequest>> GetAllTripRequestsAsync(string? status = null, int page = 1, int pageSize = 10)
        {
            var query = _context.TripRequests
                .Include(r => r.User)
                .Include(r => r.ApprovedTrip)
                    .ThenInclude(t => t!.Vehicle)
                .Include(r => r.ApprovedTrip)
                    .ThenInclude(t => t!.Driver)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(r => r.Status == status);
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTripRequestsCountAsync(string? status = null)
        {
            var query = _context.TripRequests.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                query = query.Where(r => r.Status == status);
            }
            return await query.CountAsync();
        }

        public async Task<TripRequest?> GetTripRequestByIdAsync(int id)
        {
            return await _context.TripRequests
                .Include(r => r.User)
                .Include(r => r.ApprovedTrip)
                    .ThenInclude(t => t!.Vehicle)
                .Include(r => r.ApprovedTrip)
                    .ThenInclude(t => t!.Driver)
                .FirstOrDefaultAsync(r => r.TripRequestId == id);
        }

        public async Task<(bool Success, string Message)> CreateTripRequestAsync(TripRequest request)
        {
            try
            {
                if (request.EstimatedEndDate <= request.StartDate)
                {
                    return (false, "Estimated return date must be after scheduled departure date.");
                }

                if (request.StartDate < DateTime.UtcNow.AddMinutes(-30))
                {
                    return (false, "Scheduled departure date cannot be in the past.");
                }

                request.Status = TripRequestStatus.Pending;
                request.CreatedAt = DateTime.UtcNow;

                _context.TripRequests.Add(request);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Trip request #{Id} created by User {UserId}", request.TripRequestId, request.UserId);
                return (true, "Your transport request has been submitted successfully and is pending administrator review.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trip request");
                return (false, "An error occurred while submitting your trip request. Please try again.");
            }
        }

        public async Task<(bool Success, string Message)> CancelTripRequestAsync(int id, string userId)
        {
            try
            {
                var request = await _context.TripRequests.FirstOrDefaultAsync(r => r.TripRequestId == id && r.UserId == userId);
                if (request == null)
                {
                    return (false, "Trip request not found or unauthorized.");
                }

                if (request.Status != TripRequestStatus.Pending)
                {
                    return (false, $"Cannot cancel request because it is already {request.Status}.");
                }

                request.Status = TripRequestStatus.Cancelled;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Trip request #{Id} cancelled by User {UserId}", id, userId);
                return (true, "Your trip request has been successfully cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling trip request #{Id}", id);
                return (false, "An error occurred while cancelling your trip request.");
            }
        }

        public async Task<(bool Success, string Message, int? TripId)> ApproveTripRequestAsync(int requestId, int vehicleId, int driverId, string? adminNotes, string adminUserId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.TripRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.TripRequestId == requestId);
                if (request == null)
                {
                    return (false, "Trip request not found.", null);
                }

                if (request.Status != TripRequestStatus.Pending)
                {
                    return (false, $"Request #{requestId} is not in pending status.", null);
                }

                var vehicle = await _context.Vehicles.FindAsync(vehicleId);
                if (vehicle == null)
                {
                    return (false, "Selected vehicle was not found.", null);
                }

                var driver = await _context.Drivers.FindAsync(driverId);
                if (driver == null)
                {
                    return (false, "Selected driver was not found.", null);
                }

                // Create the official scheduled trip
                var newTrip = new Trip
                {
                    VehicleId = vehicleId,
                    DriverId = driverId,
                    Source = request.Source,
                    Destination = request.Destination,
                    StartDate = request.StartDate,
                    ExpectedEndDate = request.EstimatedEndDate,
                    StartOdometer = vehicle.CurrentOdometer,
                    Purpose = $"[Approved Request #{request.TripRequestId}] {request.Purpose}",
                    TripStatus = TripStatusEnum.Scheduled,
                    CreatedAt = DateTime.UtcNow
                };

                var (tripSuccess, tripMessage) = await _tripService.CreateTripAsync(newTrip);
                if (!tripSuccess)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Could not schedule trip: {tripMessage}", null);
                }

                // Update TripRequest state
                request.Status = TripRequestStatus.Approved;
                request.ApprovedTripId = newTrip.TripId;
                request.ReviewedAt = DateTime.UtcNow;
                request.AdminNotes = adminNotes;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Proactive alerts
                await _notificationService.CreateNotificationAsync(
                    request.UserId,
                    "Trip Request Approved",
                    $"Your transport request to {request.Destination} has been approved! Vehicle {vehicle.VehicleName} ({vehicle.RegistrationNumber}) and Driver {driver.FullName} have been assigned.",
                    NotificationType.TripAssigned,
                    $"/Trips/Details/{newTrip.TripId}");

                if (!string.IsNullOrEmpty(driver.UserId))
                {
                    await _notificationService.CreateNotificationAsync(
                        driver.UserId,
                        "New Trip Scheduled",
                        $"You have been assigned to trip #{newTrip.TripId}: {request.Source} to {request.Destination}.",
                        NotificationType.TripAssigned,
                        $"/Trips/Details/{newTrip.TripId}");
                }

                _logger.LogInformation("Trip request #{RequestId} approved into Trip #{TripId}", requestId, newTrip.TripId);
                return (true, $"Trip Request #{requestId} approved and Journey #{newTrip.TripId} scheduled successfully.", newTrip.TripId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving trip request #{Id}", requestId);
                return (false, "An error occurred while approving the trip request.", null);
            }
        }

        public async Task<(bool Success, string Message)> RejectTripRequestAsync(int requestId, string reason, string adminUserId)
        {
            try
            {
                var request = await _context.TripRequests.Include(r => r.User).FirstOrDefaultAsync(r => r.TripRequestId == requestId);
                if (request == null)
                {
                    return (false, "Trip request not found.");
                }

                if (request.Status != TripRequestStatus.Pending)
                {
                    return (false, $"Cannot reject request because it is already {request.Status}.");
                }

                request.Status = TripRequestStatus.Rejected;
                request.AdminNotes = reason;
                request.ReviewedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationAsync(
                    request.UserId,
                    "Trip Request Status Update",
                    $"Your transport request to {request.Destination} could not be approved. Reason: {reason}",
                    NotificationType.General,
                    "/TripRequests");

                _logger.LogInformation("Trip request #{Id} rejected by Admin {AdminId}", requestId, adminUserId);
                return (true, "Trip request has been rejected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting trip request #{Id}", requestId);
                return (false, "An error occurred while rejecting the trip request.");
            }
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.TripRequests.CountAsync(r => r.Status == TripRequestStatus.Pending);
        }
    }
}
