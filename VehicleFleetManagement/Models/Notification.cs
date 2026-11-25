using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleFleetManagement.Models
{
    /// <summary>
    /// Represents proactive operational alerts sent to system administrators and drivers.
    /// Types include upcoming maintenance, overdue service, license expiry, insurance expiry,
    /// trip assignments, schedule changes, and vehicle fault updates.
    /// </summary>
    public class Notification
    {
        [Key]
        [Display(Name = "Notification ID")]
        public int NotificationId { get; set; }

        [Required]
        [ForeignKey("User")]
        [Display(Name = "Recipient User")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Notification title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        [Display(Name = "Alert Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Notification message is required.")]
        [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
        [Display(Name = "Alert Message")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Notification Type")]
        public string Type { get; set; } = NotificationType.General;

        [StringLength(255)]
        [Display(Name = "Action Link URL")]
        public string? ActionUrl { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Is Read")]
        public bool IsRead { get; set; } = false;

        // Navigation Properties
        public virtual ApplicationUser? User { get; set; }
    }

    /// <summary>
    /// Standard notification types for automated fleet alerts.
    /// </summary>
    public static class NotificationType
    {
        public const string MaintenanceDue = "MaintenanceDue";
        public const string OverdueService = "OverdueService";
        public const string LicenseExpiry = "LicenseExpiry";
        public const string InsuranceExpiry = "InsuranceExpiry";
        public const string TripAssigned = "TripAssigned";
        public const string TripScheduleChanged = "TripScheduleChanged";
        public const string TripCancelled = "TripCancelled";
        public const string VehicleIssueReported = "VehicleIssueReported";
        public const string VehicleIssueUpdate = "VehicleIssueUpdate";
        public const string AdminAnnouncement = "AdminAnnouncement";
        public const string General = "General";
    }
}
