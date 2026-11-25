using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleFleetManagement.Models
{
    /// <summary>
    /// Audit log tracking changes to a driver's operational availability state.
    /// Prevents fraudulent availability transitions and keeps an audit trail for fleet compliance.
    /// </summary>
    public class DriverAvailabilityLog
    {
        [Key]
        [Display(Name = "Log ID")]
        public int LogId { get; set; }

        [Required]
        [ForeignKey("Driver")]
        [Display(Name = "Driver")]
        public int DriverId { get; set; }

        [Required]
        [StringLength(25)]
        [Display(Name = "Previous Status")]
        public string PreviousStatus { get; set; } = string.Empty;

        [Required]
        [StringLength(25)]
        [Display(Name = "New Status")]
        public string NewStatus { get; set; } = string.Empty;

        [Display(Name = "Timestamp")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [StringLength(250)]
        [Display(Name = "Reason / Notes")]
        public string? Reason { get; set; }

        // Navigation Property
        public virtual Driver? Driver { get; set; }
    }
}
