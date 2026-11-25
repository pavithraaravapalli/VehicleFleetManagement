using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleFleetManagement.Models
{
    /// <summary>
    /// Represents vehicle maintenance and servicing records.
    /// Tracks preventive services, repairs, costs, and next scheduled service dates.
    /// </summary>
    public class Maintenance
    {
        [Key]
        [Display(Name = "Maintenance ID")]
        public int MaintenanceId { get; set; }

        [Required(ErrorMessage = "Please select a vehicle.")]
        [ForeignKey("Vehicle")]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Service type is required.")]
        [StringLength(50, ErrorMessage = "Service type cannot exceed 50 characters.")]
        [Display(Name = "Service Type")]
        public string ServiceType { get; set; } = string.Empty; // Routine Service, Oil Change, Brake Repair, Engine Overhaul, Tire Replacement

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "Service Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Service date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Service Date")]
        public DateTime ServiceDate { get; set; }

        [Required(ErrorMessage = "Next service due date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Next Service Due Date")]
        public DateTime NextServiceDate { get; set; }

        [Required(ErrorMessage = "Service cost is required.")]
        [Range(0, 10000000, ErrorMessage = "Cost must be a non-negative value.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Service Cost ($)")]
        public decimal Cost { get; set; }

        [Required(ErrorMessage = "Service center name is required.")]
        [StringLength(150, ErrorMessage = "Service center cannot exceed 150 characters.")]
        [Display(Name = "Service Center / Garage")]
        public string ServiceCenter { get; set; } = string.Empty;

        [Required]
        [StringLength(25)]
        [Display(Name = "Service Status")]
        public string Status { get; set; } = MaintenanceStatus.Scheduled; // Scheduled, InProgress, Completed, Overdue

        // Navigation Properties
        public virtual Vehicle? Vehicle { get; set; }
    }

    /// <summary>
    /// Standard lifecycle status constants for Maintenance events.
    /// </summary>
    public static class MaintenanceStatus
    {
        public const string Scheduled = "Scheduled";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string Overdue = "Overdue";
    }
}
