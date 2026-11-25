using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.Services.Interfaces
{
    /// <summary>
    /// Service contract for scheduling, dispatching, in-progress monitoring,
    /// conflict resolution, and completion of operational trips.
    /// </summary>
    public interface ITripService
    {
        Task<IEnumerable<Trip>> GetAllTripsAsync(string? searchTerm, string? status, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10);
        Task<int> GetTotalTripsCountAsync(string? searchTerm, string? status, DateTime? fromDate, DateTime? toDate);
        Task<IEnumerable<Trip>> GetDriverTripsAsync(int driverId, string? status = null);
        Task<Trip?> GetTripByIdAsync(int id);
        Task<(bool Success, string Message)> CreateTripAsync(Trip trip);
        Task<(bool Success, string Message)> UpdateTripAsync(Trip trip);
        Task<(bool Success, string Message)> DeleteTripAsync(int id);
        Task<(bool Success, string Message)> StartTripAsync(int tripId, decimal startOdometer, string? currentUserId = null, bool isAdmin = false);
        Task<(bool Success, string Message)> CompleteTripAsync(int tripId, decimal endOdometer, DateTime? actualEndDate = null, string? currentUserId = null, bool isAdmin = false, string? tripNotes = null);
        Task<(bool Success, string Message)> CancelTripAsync(int tripId, string? reason = null, string? currentUserId = null, bool isAdmin = false);
        Task<(bool Conflict, string Message)> CheckVehicleAvailabilityAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeTripId = null);
        Task<(bool Conflict, string Message)> CheckDriverAvailabilityAsync(int driverId, DateTime startDate, DateTime endDate, int? excludeTripId = null);
    }
}
