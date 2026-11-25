using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.ViewModels
{
    /// <summary>
    /// ViewModel for scheduling and creating a new operational trip.
    /// </summary>
    public class CreateTripViewModel
    {
        [Required(ErrorMessage = "Please select a vehicle.")]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Please select an authorized driver.")]
        [Display(Name = "Driver")]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "Origin / Source location is required.")]
        [StringLength(100, ErrorMessage = "Source cannot exceed 100 characters.")]
        [Display(Name = "Origin / Source")]
        public string Source { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination location is required.")]
        [StringLength(100, ErrorMessage = "Destination cannot exceed 100 characters.")]
        [Display(Name = "Destination")]
        public string Destination { get; set; } = string.Empty;

        [Required(ErrorMessage = "Departure date and time are required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Scheduled Departure")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddHours(1);

        [Required(ErrorMessage = "Expected arrival date and time are required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Expected Arrival")]
        public DateTime ExpectedEndDate { get; set; } = DateTime.UtcNow.AddHours(5);

        [Required(ErrorMessage = "Initial odometer reading is required.")]
        [Range(0, 1000000, ErrorMessage = "Start odometer reading must be non-negative.")]
        [Display(Name = "Start Odometer (km)")]
        public decimal StartOdometer { get; set; }

        [Required(ErrorMessage = "Trip purpose is required.")]
        [StringLength(250, ErrorMessage = "Purpose cannot exceed 250 characters.")]
        [Display(Name = "Trip Purpose")]
        public string Purpose { get; set; } = string.Empty;

        // Select lists for dropdown population
        public IEnumerable<SelectListItem>? AvailableVehicles { get; set; }
        public IEnumerable<SelectListItem>? AvailableDrivers { get; set; }
    }

    /// <summary>
    /// ViewModel for driver or admin to start a scheduled journey.
    /// </summary>
    public class StartTripViewModel
    {
        public int TripId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;

        [Required(ErrorMessage = "Current vehicle odometer reading is required.")]
        [Range(0, 1000000, ErrorMessage = "Odometer reading must be positive.")]
        [Display(Name = "Starting Odometer (km)")]
        public decimal StartOdometer { get; set; }
    }

    /// <summary>
    /// ViewModel for completing a journey with final odometer reading, trip notes, and distance calculation.
    /// </summary>
    public class CompleteTripViewModel
    {
        public int TripId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public decimal StartOdometer { get; set; }

        [Required(ErrorMessage = "Final odometer reading is required.")]
        [Range(0, 1000000, ErrorMessage = "End odometer reading must be positive.")]
        [Display(Name = "Final End Odometer (km)")]
        public decimal EndOdometer { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Actual Arrival Date & Time")]
        public DateTime ActualEndDate { get; set; } = DateTime.UtcNow;

        [StringLength(500, ErrorMessage = "Trip notes cannot exceed 500 characters.")]
        [Display(Name = "Driver Journey Notes / Remarks")]
        public string? TripNotes { get; set; }
    }
}
