using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.ViewModels;

namespace VehicleFleetManagement.Services.Interfaces
{
    /// <summary>
    /// Service contract for fuel purchase logging, financial expense aggregation,
    /// mileage efficiency calculations (km/L), and monthly summaries.
    /// </summary>
    public interface IFuelService
    {
        Task<IEnumerable<FuelLog>> GetAllFuelLogsAsync(int? vehicleId, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10);
        Task<int> GetTotalCountAsync(int? vehicleId, DateTime? fromDate, DateTime? toDate);
        Task<FuelLog?> GetByIdAsync(int id);
        Task<(bool Success, string Message)> CreateFuelLogAsync(FuelLog fuelLog);
        Task<(bool Success, string Message)> UpdateFuelLogAsync(FuelLog fuelLog);
        Task<(bool Success, string Message)> DeleteFuelLogAsync(int id);
        Task<decimal> GetTotalFuelExpensesAsync(int? vehicleId = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<decimal?> CalculateMileageAsync(int fuelLogId);
        Task<IEnumerable<MonthlyFuelExpenseSummary>> GetMonthlyFuelExpensesAsync(int year);
    }
}
