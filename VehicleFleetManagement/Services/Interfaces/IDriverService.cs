using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.Services.Interfaces
{
    /// <summary>
    /// Service contract for Driver credentials, license audits, search, and trip assignment history.
    /// </summary>
    public interface IDriverService
    {
        Task<IEnumerable<Driver>> GetAllDriversAsync(string? searchTerm, string? availabilityStatus, int page = 1, int pageSize = 10);
        Task<int> GetTotalDriverCountAsync(string? searchTerm, string? availabilityStatus);
        Task<Driver?> GetDriverByIdAsync(int id);
        Task<Driver?> GetDriverWithHistoryAsync(int id);
        Task<(bool Success, string Message)> CreateDriverAsync(Driver driver);
        Task<(bool Success, string Message)> UpdateDriverAsync(Driver driver);
        Task<(bool Success, string Message)> DeleteOrDeactivateDriverAsync(int id);
        Task<IEnumerable<Driver>> GetAvailableDriversAsync();
    }
}
