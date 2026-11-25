using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.ViewModels
{
    /// <summary>
    /// ViewModel for logging and editing fuel purchase receipts.
    /// </summary>
    public class CreateFuelLogViewModel
    {
        public int FuelLogId { get; set; }

        [Required(ErrorMessage = "Please select a vehicle.")]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }

        [Display(Name = "Associated Trip (Optional)")]
        public int? TripId { get; set; }

        [Required(ErrorMessage = "Fuel quantity is required.")]
        [Range(0.01, 2000.00, ErrorMessage = "Fuel quantity must be between 0.01 and 2,000 litres.")]
        [Display(Name = "Fuel Quantity (Litres)")]
        public decimal FuelQuantity { get; set; }

        [Required(ErrorMessage = "Price per litre is required.")]
        [Range(0.01, 1000.00, ErrorMessage = "Price per litre must be between $0.01 and $1,000.00.")]
        [Display(Name = "Price Per Litre ($)")]
        public decimal PricePerLitre { get; set; }

        [Display(Name = "Total Cost ($)")]
        public decimal TotalCost { get; set; }

        [Required(ErrorMessage = "Fuel purchase date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Purchase Date")]
        public DateTime FuelDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Fuel station name is required.")]
        [StringLength(150, ErrorMessage = "Fuel station cannot exceed 150 characters.")]
        [Display(Name = "Fuel Station / Vendor Name")]
        public string FuelStation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Odometer reading at fuel refill is required.")]
        [Range(0, 1000000, ErrorMessage = "Odometer reading must be non-negative.")]
        [Display(Name = "Odometer at Refill (km)")]
        public decimal OdometerReading { get; set; }

        public IEnumerable<SelectListItem>? AvailableVehicles { get; set; }
        public IEnumerable<SelectListItem>? AvailableTrips { get; set; }
    }

    /// <summary>
    /// Index ViewModel providing fuel expense analytics and historical records.
    /// </summary>
    public class FuelIndexViewModel
    {
        public IEnumerable<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();
        public decimal TotalFuelCost { get; set; }
        public decimal TotalLitres { get; set; }
        public decimal AveragePricePerLitre { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? SelectedVehicleId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// DTO representing aggregated monthly fuel expenditures.
    /// </summary>
    public class MonthlyFuelExpenseSummary
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public decimal TotalQuantity { get; set; }
        public int RefillCount { get; set; }
        public decimal AveragePricePerLitre => TotalQuantity > 0 ? TotalCost / TotalQuantity : 0;
    }
}
