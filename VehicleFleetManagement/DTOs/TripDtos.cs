using System;
using System.ComponentModel.DataAnnotations;

namespace VehicleFleetManagement.DTOs
{
    /// <summary>
    /// Data Transfer Object representing detailed trip itinerary data.
    /// Excludes navigational loops while providing vehicle and driver summary metadata.
    /// </summary>
    public class TripDto
    {
        public int TripId { get; set; }
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime ExpectedEndDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public decimal StartOdometer { get; set; }
        public decimal? EndOdometer { get; set; }
        public decimal Distance { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string TripStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Input DTO for scheduling a new trip.
    /// </summary>
    public class CreateTripDto
    {
        [Required(ErrorMessage = "Vehicle ID is required.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Driver ID is required.")]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "Trip origin/source is required.")]
        [StringLength(100, ErrorMessage = "Source cannot exceed 100 characters.")]
        public string Source { get; set; } = string.Empty;

        [Required(ErrorMessage = "Trip destination is required.")]
        [StringLength(100, ErrorMessage = "Destination cannot exceed 100 characters.")]
        public string Destination { get; set; } = string.Empty;

        [Required(ErrorMessage = "Scheduled start date is required.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Expected arrival date is required.")]
        public DateTime ExpectedEndDate { get; set; }

        [Required(ErrorMessage = "Start odometer reading is required.")]
        [Range(0, 1000000, ErrorMessage = "Start odometer must be non-negative.")]
        public decimal StartOdometer { get; set; }

        [Required(ErrorMessage = "Trip purpose is required.")]
        [StringLength(250, ErrorMessage = "Purpose cannot exceed 250 characters.")]
        public string Purpose { get; set; } = string.Empty;
    }

    /// <summary>
    /// Input DTO for initiating/starting a scheduled trip.
    /// </summary>
    public class StartTripDto
    {
        [Required(ErrorMessage = "Start odometer reading is required.")]
        [Range(0, 1000000, ErrorMessage = "Start odometer must be non-negative.")]
        public decimal StartOdometer { get; set; }
    }

    /// <summary>
    /// Input DTO for concluding a completed trip.
    /// </summary>
    public class CompleteTripDto
    {
        [Required(ErrorMessage = "End odometer reading is required.")]
        [Range(0, 1000000, ErrorMessage = "End odometer must be non-negative.")]
        public decimal EndOdometer { get; set; }
    }

    /// <summary>
    /// Input DTO for updating an existing scheduled trip.
    /// </summary>
    public class UpdateTripDto
    {
        [Required]
        public int TripId { get; set; }

        [Required(ErrorMessage = "Vehicle ID is required.")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Driver ID is required.")]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "Source is required.")]
        [StringLength(100, ErrorMessage = "Source cannot exceed 100 characters.")]
        public string Source { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination is required.")]
        [StringLength(100, ErrorMessage = "Destination cannot exceed 100 characters.")]
        public string Destination { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Expected arrival date is required.")]
        public DateTime ExpectedEndDate { get; set; }

        [Required(ErrorMessage = "Start odometer is required.")]
        [Range(0, 1000000, ErrorMessage = "Start odometer must be non-negative.")]
        public decimal StartOdometer { get; set; }

        [Required(ErrorMessage = "Trip purpose is required.")]
        [StringLength(250, ErrorMessage = "Purpose cannot exceed 250 characters.")]
        public string Purpose { get; set; } = string.Empty;

        [Required(ErrorMessage = "Trip status is required.")]
        public string TripStatus { get; set; } = string.Empty;
    }
}
