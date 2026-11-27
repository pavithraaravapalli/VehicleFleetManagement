using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleFleetManagement.Models
{
    /// <summary>
    /// Represents a formal transport or vehicle booking request submitted by an organizational staff member.
    /// Handled by Fleet Administrators who review, approve (assigning vehicle and driver), or reject the request.
    /// </summary>
    public class TripRequest
    {
        [Key]
        [Display(Name = "Request ID")]
        public int TripRequestId { get; set; }

        [Required]
        [ForeignKey("User")]
        [Display(Name = "Requesting User")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Origin / Pickup location is required.")]
        [StringLength(100, ErrorMessage = "Origin cannot exceed 100 characters.")]
        [Display(Name = "Origin / Pickup Location")]
        public string Source { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination location is required.")]
        [StringLength(100, ErrorMessage = "Destination cannot exceed 100 characters.")]
        [Display(Name = "Destination Location")]
        public string Destination { get; set; } = string.Empty;

        [Required(ErrorMessage = "Scheduled departure date and time are required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Departure Date & Time")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Estimated return date and time are required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Estimated Return Date & Time")]
        public DateTime EstimatedEndDate { get; set; }

        [Required(ErrorMessage = "Please specify number of passengers.")]
        [Range(1, 100, ErrorMessage = "Passenger count must be between 1 and 100.")]
        [Display(Name = "Passenger Count")]
        public int PassengerCount { get; set; } = 1;

        [StringLength(50)]
        [Display(Name = "Preferred Vehicle Type")]
        public string? PreferredVehicleType { get; set; } = "Any"; // Sedan, SUV, Van, Bus, Truck, Any

        [Required(ErrorMessage = "Trip purpose / official justification is required.")]
        [StringLength(500, ErrorMessage = "Purpose cannot exceed 500 characters.")]
        [Display(Name = "Official Purpose / Justification")]
        public string Purpose { get; set; } = string.Empty;

        [Required]
        [StringLength(25)]
        [Display(Name = "Request Status")]
        public string Status { get; set; } = TripRequestStatus.Pending; // Pending, Approved, Rejected, Cancelled

        [StringLength(500)]
        [Display(Name = "Administrator Review Notes")]
        public string? AdminNotes { get; set; }

        [ForeignKey("ApprovedTrip")]
        [Display(Name = "Generated Fleet Trip")]
        public int? ApprovedTripId { get; set; }

        [Display(Name = "Request Submission Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Review Date")]
        public DateTime? ReviewedAt { get; set; }

        // Navigation Properties
        public virtual ApplicationUser? User { get; set; }
        public virtual Trip? ApprovedTrip { get; set; }
    }

    /// <summary>
    /// Lifecycle status constants for Trip Requests.
    /// </summary>
    public static class TripRequestStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Cancelled = "Cancelled";
    }
}
