using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.Services.Interfaces
{
    /// <summary>
    /// Service contract for vehicle maintenance scheduling, repair logs,
    /// 7-day proactive due alerts, and service center cost tracking.
    /// </summary>
    public interface IMaintenanceService
    {
        Task<IEnumerable<Maintenance>> GetAllMaintenanceAsync(string? searchTerm, string? status, int? vehicleId, int page = 1, int pageSize = 10);
        Task<int> GetTotalCountAsync(string? searchTerm, string? status, int? vehicleId);
        Task<Maintenance?> GetByIdAsync(int id);
        Task<IEnumerable<Maintenance>> GetUpcomingMaintenanceAlertsAsync(int daysThreshold = 7);
        Task<IEnumerable<Maintenance>> GetOverdueMaintenanceAsync();
        Task<(bool Success, string Message)> CreateMaintenanceAsync(Maintenance maintenance);
        Task<(bool Success, string Message)> UpdateMaintenanceAsync(Maintenance maintenance);
        Task<(bool Success, string Message)> DeleteMaintenanceAsync(int id);
        Task<decimal> GetTotalMaintenanceCostAsync();
    }
}
