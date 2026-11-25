using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleFleetManagement.Models
{
    /// <summary>
    /// Represents a fuel purchase event for a vehicle, optionally linked to a specific trip.
    /// Used to calculate total fuel expenses, consumption trends, and fleet mileage efficiency.
    /// </summary>
    public class FuelLog
    {
        [Key]
        [Display(Name = "Fuel Log ID")]
        public int FuelLogId { get; set; }

        [Required(ErrorMessage = "Please select a vehicle.")]
        [ForeignKey("Vehicle")]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }

        [ForeignKey("Trip")]
        [Display(Name = "Associated Trip (Optional)")]
        public int? TripId { get; set; }

        [Required(ErrorMessage = "Fuel quantity is required.")]
        [Range(0.01, 2000.00, ErrorMessage = "Fuel quantity must be between 0.01 and 2,000 litres.")]
        [Column(TypeName = "decimal(8,2)")]
        [Display(Name = "Fuel Quantity (Litres)")]
        public decimal FuelQuantity { get; set; }

        [Required(ErrorMessage = "Price per litre is required.")]
        [Range(0.01, 1000.00, ErrorMessage = "Price per litre must be between 0.01 and 1,000.")]
        [Column(TypeName = "decimal(8,2)")]
        [Display(Name = "Price Per Litre ($)")]
        public decimal PricePerLitre { get; set; }

        [Required]
        [Range(0.01, 1000000.00, ErrorMessage = "Total cost must be positive.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Cost ($)")]
        public decimal TotalCost { get; set; }

        [Required(ErrorMessage = "Fuel purchase date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fuel Purchase Date")]
        public DateTime FuelDate { get; set; }

        [Required(ErrorMessage = "Fuel station name is required.")]
        [StringLength(150, ErrorMessage = "Fuel station cannot exceed 150 characters.")]
        [Display(Name = "Fuel Station")]
        public string FuelStation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Odometer reading at fuel refill is required.")]
        [Range(0, 1000000, ErrorMessage = "Odometer reading must be non-negative.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Odometer at Refill (km)")]
        public decimal OdometerReading { get; set; }

        // Navigation Properties
        public virtual Vehicle? Vehicle { get; set; }
        public virtual Trip? Trip { get; set; }
    }
}
