using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleFleetManagement.Models
{
    /// <summary>
    /// Represents an operational journey scheduled and executed within the fleet.
    /// Tracks vehicle assignment, assigned driver, departure/arrival schedules,
    /// start/end odometer readings, and overall distance covered.
    /// </summary>
    public class Trip
    {
        [Key]
        [Display(Name = "Trip ID")]
        public int TripId { get; set; }

        [Required(ErrorMessage = "Please select a vehicle.")]
        [ForeignKey("Vehicle")]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Please select a driver.")]
        [ForeignKey("Driver")]
        [Display(Name = "Driver")]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "Trip origin/source location is required.")]
        [StringLength(100, ErrorMessage = "Source cannot exceed 100 characters.")]
        [Display(Name = "Origin / Source")]
        public string Source { get; set; } = string.Empty;

        [Required(ErrorMessage = "Trip destination location is required.")]
        [StringLength(100, ErrorMessage = "Destination cannot exceed 100 characters.")]
        [Display(Name = "Destination")]
        public string Destination { get; set; } = string.Empty;

        [Required(ErrorMessage = "Departure start date and time are required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Scheduled Start Date & Time")]
        public DateTime StartDate { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Actual Start Date & Time")]
        public DateTime? ActualStartDate { get; set; }

        [Required(ErrorMessage = "Expected arrival date and time are required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Expected End Date & Time")]
        public DateTime ExpectedEndDate { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Actual End Date & Time")]
        public DateTime? ActualEndDate { get; set; }

        [Required(ErrorMessage = "Start odometer reading is required.")]
        [Range(0, 1000000, ErrorMessage = "Start odometer reading must be positive.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Start Odometer (km)")]
        public decimal StartOdometer { get; set; }

        [Range(0, 1000000, ErrorMessage = "End odometer reading must be positive.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "End Odometer (km)")]
        public decimal? EndOdometer { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Distance Travelled (km)")]
        public decimal Distance { get; set; } = 0;

        [Required(ErrorMessage = "Trip purpose is required.")]
        [StringLength(250, ErrorMessage = "Purpose cannot exceed 250 characters.")]
        [Display(Name = "Trip Purpose")]
        public string Purpose { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Driver Trip Notes")]
        public string? TripNotes { get; set; }

        [Required]
        [StringLength(25)]
        [Display(Name = "Trip Status")]
        public string TripStatus { get; set; } = TripStatusEnum.Scheduled; // Scheduled, InProgress, Completed, Cancelled

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Vehicle? Vehicle { get; set; }
        public virtual Driver? Driver { get; set; }
        public virtual ICollection<FuelLog> FuelLogs { get; set; } = new List<FuelLog>();
        public virtual ICollection<DriverReview> Reviews { get; set; } = new List<DriverReview>();
        public virtual ICollection<VehicleIssueReport> IssueReports { get; set; } = new List<VehicleIssueReport>();
    }

    /// <summary>
    /// Standard lifecycle statuses for Trip management.
    /// </summary>
    public static class TripStatusEnum
    {
        public const string Scheduled = "Scheduled";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
    }
}
