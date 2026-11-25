using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.ViewModels
{
    /// <summary>
    /// Base filter parameters used across multiple report queries.
    /// </summary>
    public class ReportFilterViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? VehicleId { get; set; }
        public int? DriverId { get; set; }
        public string? Status { get; set; }
        public int? Year { get; set; }

        public IEnumerable<SelectListItem>? AvailableVehicles { get; set; }
        public IEnumerable<SelectListItem>? AvailableDrivers { get; set; }
        public IEnumerable<SelectListItem>? AvailableStatuses { get; set; }
    }

    /// <summary>
    /// Vehicle Utilization Report ViewModel.
    /// Analyzes fleet asset usage, trip frequency, total kilometers driven, and utilization rate.
    /// </summary>
    public class VehicleUtilizationReportViewModel
    {
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public int TotalTrips { get; set; }
        public int CompletedTrips { get; set; }
        public decimal TotalDistanceKm { get; set; }
        public decimal CurrentOdometer { get; set; }
        public int ActiveDaysInPeriod { get; set; }
        public decimal UtilizationPercentage { get; set; }
    }

    /// <summary>
    /// Driver Trip History Report ViewModel.
    /// Aggregates individual driver assignment counts, distance driven, and performance track record.
    /// </summary>
    public class DriverTripHistoryReportViewModel
    {
        public int DriverId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string AvailabilityStatus { get; set; } = string.Empty;
        public int TotalAssignedTrips { get; set; }
        public int CompletedTrips { get; set; }
        public int InProgressTrips { get; set; }
        public int CancelledTrips { get; set; }
        public decimal TotalDistanceDrivenKm { get; set; }
        public IEnumerable<Trip> RecentTrips { get; set; } = new List<Trip>();
    }

    /// <summary>
    /// Trip Status Report ViewModel (Completed / Pending Trips).
    /// </summary>
    public class TripsReportViewModel
    {
        public string ReportTitle { get; set; } = "Trips Report";
        public string StatusFilter { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int TotalCount { get; set; }
        public decimal TotalDistanceKm { get; set; }
        public IEnumerable<Trip> Trips { get; set; } = new List<Trip>();
    }

    /// <summary>
    /// Monthly Fuel Expenses Report ViewModel.
    /// </summary>
    public class MonthlyFuelExpensesReportViewModel
    {
        public int SelectedYear { get; set; }
        public decimal AnnualTotalCost { get; set; }
        public decimal AnnualTotalLitres { get; set; }
        public int AnnualRefillCount { get; set; }
        public decimal AveragePricePerLitre => AnnualTotalLitres > 0 ? AnnualTotalCost / AnnualTotalLitres : 0;
        public IEnumerable<MonthlyFuelExpenseSummary> MonthlySummaries { get; set; } = new List<MonthlyFuelExpenseSummary>();
    }

    /// <summary>
    /// Vehicle Maintenance Expense Report ViewModel.
    /// Categorizes maintenance costs by vehicle and service type.
    /// </summary>
    public class MaintenanceExpenseReportViewModel
    {
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;
        public int ServiceCount { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal AverageServiceExpense => ServiceCount > 0 ? TotalExpense / ServiceCount : 0;
        public IEnumerable<Maintenance> MaintenanceRecords { get; set; } = new List<Maintenance>();
    }

    /// <summary>
    /// Fuel Consumption and Mileage Efficiency Report ViewModel.
    /// Compares vehicle fuel volume, total expenditure, and calculated efficiency (km/L).
    /// </summary>
    public class FuelConsumptionReportViewModel
    {
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;
        public string FuelType { get; set; } = string.Empty;
        public int RefillCount { get; set; }
        public decimal TotalLitres { get; set; }
        public decimal TotalFuelCost { get; set; }
        public decimal AveragePricePerLitre => TotalLitres > 0 ? TotalFuelCost / TotalLitres : 0;
        public decimal AverageMileageKmPerLitre { get; set; }
        public IEnumerable<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();
    }
}
