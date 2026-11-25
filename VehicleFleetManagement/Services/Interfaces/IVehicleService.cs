using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.Services.Interfaces
{
    /// <summary>
    /// Service contract for Vehicle asset lifecycle, search, pagination, and relational history.
    /// </summary>
    public interface IVehicleService
    {
        Task<IEnumerable<Vehicle>> GetAllVehiclesAsync(string? searchTerm, string? vehicleType, string? status, int page = 1, int pageSize = 10);
        Task<int> GetTotalVehicleCountAsync(string? searchTerm, string? vehicleType, string? status);
        Task<Vehicle?> GetVehicleByIdAsync(int id);
        Task<Vehicle?> GetVehicleWithHistoryAsync(int id);
        Task<(bool Success, string Message)> CreateVehicleAsync(Vehicle vehicle);
        Task<(bool Success, string Message)> UpdateVehicleAsync(Vehicle vehicle);
        Task<(bool Success, string Message)> DeleteVehicleAsync(int id);
        Task<IEnumerable<string>> GetDistinctVehicleTypesAsync();
        Task<IEnumerable<Vehicle>> GetAvailableVehiclesAsync();
    }
}
