using System;
using System.ComponentModel.DataAnnotations;

namespace VehicleFleetManagement.DTOs
{
    /// <summary>
    /// Data Transfer Object representing public vehicle details.
    /// Excludes internal navigational entities to prevent circular reference cycles.
    /// </summary>
    public class VehicleDto
    {
        public int VehicleId { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string FuelType { get; set; } = string.Empty;
        public int ManufacturingYear { get; set; }
        public int SeatingCapacity { get; set; }
        public decimal CurrentOdometer { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime InsuranceExpiryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Input DTO for registering a new vehicle asset.
    /// </summary>
    public class CreateVehicleDto
    {
        [Required(ErrorMessage = "Registration number is required.")]
        [StringLength(20, ErrorMessage = "Registration number cannot exceed 20 characters.")]
        [RegularExpression(@"^[A-Z0-9\s\-]+$", ErrorMessage = "Registration number can only contain uppercase letters, numbers, spaces, and hyphens.")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle name is required.")]
        [StringLength(50, ErrorMessage = "Vehicle name cannot exceed 50 characters.")]
        public string VehicleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50, ErrorMessage = "Model cannot exceed 50 characters.")]
        public string Model { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle type is required.")]
        [StringLength(30, ErrorMessage = "Vehicle type cannot exceed 30 characters.")]
        public string VehicleType { get; set; } = string.Empty; // Sedan, SUV, Truck, Van, Bus

        [Required(ErrorMessage = "Fuel type is required.")]
        [StringLength(20, ErrorMessage = "Fuel type cannot exceed 20 characters.")]
        public string FuelType { get; set; } = string.Empty; // Petrol, Diesel, Electric, Hybrid, CNG

        [Required(ErrorMessage = "Manufacturing year is required.")]
        [Range(1990, 2030, ErrorMessage = "Manufacturing year must be between 1990 and 2030.")]
        public int ManufacturingYear { get; set; }

        [Required(ErrorMessage = "Seating capacity is required.")]
        [Range(1, 100, ErrorMessage = "Seating capacity must be between 1 and 100.")]
        public int SeatingCapacity { get; set; }

        [Required(ErrorMessage = "Current odometer reading is required.")]
        [Range(0, 1000000, ErrorMessage = "Odometer reading must be non-negative.")]
        public decimal CurrentOdometer { get; set; }

        [Required(ErrorMessage = "Registration date is required.")]
        public DateTime RegistrationDate { get; set; }

        [Required(ErrorMessage = "Insurance expiry date is required.")]
        public DateTime InsuranceExpiryDate { get; set; }
    }

    /// <summary>
    /// Input DTO for updating existing vehicle asset specifications.
    /// </summary>
    public class UpdateVehicleDto
    {
        [Required]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Registration number is required.")]
        [StringLength(20, ErrorMessage = "Registration number cannot exceed 20 characters.")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle name is required.")]
        [StringLength(50, ErrorMessage = "Vehicle name cannot exceed 50 characters.")]
        public string VehicleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50, ErrorMessage = "Model cannot exceed 50 characters.")]
        public string Model { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle type is required.")]
        [StringLength(30, ErrorMessage = "Vehicle type cannot exceed 30 characters.")]
        public string VehicleType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Fuel type is required.")]
        [StringLength(20, ErrorMessage = "Fuel type cannot exceed 20 characters.")]
        public string FuelType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Manufacturing year is required.")]
        [Range(1990, 2030, ErrorMessage = "Manufacturing year must be between 1990 and 2030.")]
        public int ManufacturingYear { get; set; }

        [Required(ErrorMessage = "Seating capacity is required.")]
        [Range(1, 100, ErrorMessage = "Seating capacity must be between 1 and 100.")]
        public int SeatingCapacity { get; set; }

        [Required(ErrorMessage = "Current odometer reading is required.")]
        [Range(0, 1000000, ErrorMessage = "Odometer reading must be non-negative.")]
        public decimal CurrentOdometer { get; set; }

        [Required(ErrorMessage = "Registration date is required.")]
        public DateTime RegistrationDate { get; set; }

        [Required(ErrorMessage = "Insurance expiry date is required.")]
        public DateTime InsuranceExpiryDate { get; set; }

        [Required(ErrorMessage = "Operational status is required.")]
        public string Status { get; set; } = string.Empty; // Available, Assigned, UnderMaintenance
    }
}
