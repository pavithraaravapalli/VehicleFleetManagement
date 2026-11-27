using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace VehicleFleetManagement.Models
{
    /// <summary>
    /// Extends the ASP.NET Core IdentityUser with application-specific user profile attributes.
    /// Used for role-based authentication (Admin, Driver, User) and system audits.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Profile Picture")]
        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "Account Created Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<DriverReview> ReviewsWritten { get; set; } = new List<DriverReview>();
        public virtual ICollection<TripRequest> TripRequests { get; set; } = new List<TripRequest>();
        public virtual Driver? Driver { get; set; }
    }
}
