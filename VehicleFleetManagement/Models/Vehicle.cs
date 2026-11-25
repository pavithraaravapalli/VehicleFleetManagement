using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleFleetManagement.Models
{
    /// <summary>
    /// Represents a vehicle in the fleet management system.
    /// Tracks registration, specifications, maintenance lifecycle, and current operational status.
    /// </summary>
    public class Vehicle
    {
        [Key]
        [Display(Name = "Vehicle ID")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Registration number is required.")]
        [StringLength(20, ErrorMessage = "Registration number cannot exceed 20 characters.")]
        [RegularExpression(@"^[A-Z0-9\s\-]+$", ErrorMessage = "Registration number can only contain uppercase letters, numbers, spaces, and hyphens.")]
        [Display(Name = "Registration Number")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle name is required.")]
        [StringLength(50, ErrorMessage = "Vehicle name cannot exceed 50 characters.")]
        [Display(Name = "Vehicle Name / Make")]
        public string VehicleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50, ErrorMessage = "Model cannot exceed 50 characters.")]
        [Display(Name = "Model")]
        public string Model { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle type is required.")]
        [StringLength(30, ErrorMessage = "Vehicle type cannot exceed 30 characters.")]
        [Display(Name = "Vehicle Type")]
        public string VehicleType { get; set; } = string.Empty; // Sedan, SUV, Truck, Van, Bus

        [Required(ErrorMessage = "Fuel type is required.")]
        [StringLength(20, ErrorMessage = "Fuel type cannot exceed 20 characters.")]
        [Display(Name = "Fuel Type")]
        public string FuelType { get; set; } = string.Empty; // Petrol, Diesel, Electric, Hybrid, CNG

        [Required(ErrorMessage = "Manufacturing year is required.")]
        [Range(1990, 2030, ErrorMessage = "Manufacturing year must be between 1990 and 2030.")]
        [Display(Name = "Manufacturing Year")]
        public int ManufacturingYear { get; set; }

        [Required(ErrorMessage = "Seating capacity is required.")]
        [Range(1, 100, ErrorMessage = "Seating capacity must be between 1 and 100.")]
        [Display(Name = "Seating Capacity")]
        public int SeatingCapacity { get; set; }

        [Required(ErrorMessage = "Current odometer reading is required.")]
        [Range(0, 1000000, ErrorMessage = "Current odometer reading must be between 0 and 1,000,000 km.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Current Odometer (km)")]
        public decimal CurrentOdometer { get; set; }

        [Required(ErrorMessage = "Registration date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; }

        [Required(ErrorMessage = "Insurance expiry date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Insurance Expiry Date")]
        public DateTime InsuranceExpiryDate { get; set; }

        [Required]
        [StringLength(25)]
        [Display(Name = "Vehicle Status")]
        public string Status { get; set; } = VehicleStatus.Available; // Available, Assigned, UnderMaintenance, Inactive

        [Display(Name = "Record Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
        public virtual ICollection<Maintenance> Maintenances { get; set; } = new List<Maintenance>();
        public virtual ICollection<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();
    }

    /// <summary>
    /// Standard status constants for Vehicle lifecycle.
    /// </summary>
    public static class VehicleStatus
    {
        public const string Available = "Available";
        public const string Assigned = "Assigned";
        public const string UnderMaintenance = "UnderMaintenance";
        public const string Inactive = "Inactive";
    }
}
