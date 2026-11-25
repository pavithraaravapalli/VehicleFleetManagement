using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleFleetManagement.Models
{
    /// <summary>
    /// Represents an authorized fleet driver.
    /// Manages driver credentials, license validation, and trip assignment availability.
    /// Can be associated with an ApplicationUser login account.
    /// </summary>
    public class Driver
    {
        [Key]
        [Display(Name = "Driver ID")]
        public int DriverId { get; set; }

        [ForeignKey("User")]
        [Display(Name = "Associated User Account")]
        public string? UserId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Driving license number is required.")]
        [StringLength(30, ErrorMessage = "License number cannot exceed 30 characters.")]
        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "License expiry date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "License Expiry Date")]
        public DateTime LicenseExpiryDate { get; set; }

        [Required(ErrorMessage = "Residential address is required.")]
        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Joining date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Joining Date")]
        public DateTime JoiningDate { get; set; }

        [Required]
        [StringLength(25)]
        [Display(Name = "Availability Status")]
        public string AvailabilityStatus { get; set; } = DriverStatus.Available; // Available, OnTrip, Unavailable, OnLeave, Inactive, Suspended

        [StringLength(255)]
        [Display(Name = "Profile Picture")]
        public string? ProfilePictureUrl { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        [Range(1.0, 5.0)]
        [Display(Name = "Average Rating")]
        public decimal Rating { get; set; } = 5.00m;

        [Display(Name = "Total Reviews")]
        public int TotalReviews { get; set; } = 0;

        // Navigation Properties
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
        public virtual ICollection<DriverReview> Reviews { get; set; } = new List<DriverReview>();
        public virtual ICollection<VehicleIssueReport> ReportedIssues { get; set; } = new List<VehicleIssueReport>();
        public virtual ICollection<DriverAvailabilityLog> AvailabilityLogs { get; set; } = new List<DriverAvailabilityLog>();
    }

    /// <summary>
    /// Standard availability status constants for Driver lifecycle.
    /// </summary>
    public static class DriverStatus
    {
        public const string Available = "Available";
        public const string OnTrip = "OnTrip";
        public const string Unavailable = "Unavailable";
        public const string OnLeave = "OnLeave";
        public const string Inactive = "Inactive";
        public const string Suspended = "Suspended";
    }
}
