using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.Services.Interfaces
{
    /// <summary>
    /// Service contract for handling user transport booking requests,
    /// administrator reviews, approvals (converting request to scheduled Trip), and rejections.
    /// </summary>
    public interface ITripRequestService
    {
        Task<IEnumerable<TripRequest>> GetUserTripRequestsAsync(string userId);
        Task<IEnumerable<TripRequest>> GetAllTripRequestsAsync(string? status = null, int page = 1, int pageSize = 10);
        Task<int> GetTripRequestsCountAsync(string? status = null);
        Task<TripRequest?> GetTripRequestByIdAsync(int id);
        Task<(bool Success, string Message)> CreateTripRequestAsync(TripRequest request);
        Task<(bool Success, string Message)> CancelTripRequestAsync(int id, string userId);
        Task<(bool Success, string Message, int? TripId)> ApproveTripRequestAsync(int requestId, int vehicleId, int driverId, string? adminNotes, string adminUserId);
        Task<(bool Success, string Message)> RejectTripRequestAsync(int requestId, string reason, string adminUserId);
        Task<int> GetPendingCountAsync();
    }
}
