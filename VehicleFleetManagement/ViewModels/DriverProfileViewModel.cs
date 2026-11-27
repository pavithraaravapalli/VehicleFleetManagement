using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.ViewModels
{
    /// <summary>
    /// ViewModel for managing a driver's personal profile, photo upload,
    /// availability transitions, credentials, and review distribution.
    /// </summary>
    public class DriverProfileViewModel
    {
        public int DriverId { get; set; }
        public string? UserId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Residential address is required.")]
        [StringLength(250)]
        [Display(Name = "Residential Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; } = string.Empty;

        public string MaskedLicenseNumber
        {
            get
            {
                if (string.IsNullOrEmpty(LicenseNumber) || LicenseNumber.Length <= 4)
                    return "****";
                return string.Concat(new string('*', LicenseNumber.Length - 4), LicenseNumber.Substring(LicenseNumber.Length - 4));
            }
        }

        [Display(Name = "License Expiry Date")]
        public DateTime LicenseExpiryDate { get; set; }

        [Display(Name = "Joining Date")]
        public DateTime JoiningDate { get; set; }

        [Display(Name = "Availability Status")]
        public string AvailabilityStatus { get; set; } = DriverStatus.Available;

        [Display(Name = "Profile Photo")]
        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "Upload New Photo (Max 3MB, .jpg/.png/.webp)")]
        public IFormFile? PhotoUpload { get; set; }

        // Professional Telemetry
        public decimal Rating { get; set; } = 5.0m;
        public int TotalReviews { get; set; }
        public int CompletedTripsCount { get; set; }
        public decimal TotalDistanceTravelled { get; set; }
        public Vehicle? CurrentlyAssignedVehicle { get; set; }

        // Review Distribution (Count per Star 1 to 5)
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }

        // Reviews Collection
        public List<DriverReview> Reviews { get; set; } = new List<DriverReview>();

        // Driver Milestones & Badges
        public List<DriverAchievement> Achievements { get; set; } = new List<DriverAchievement>();
    }

    /// <summary>
    /// Represents a verified performance milestone or professional badge earned by a driver.
    /// </summary>
    public class DriverAchievement
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsEarned { get; set; }
        public string BadgeClass { get; set; } = string.Empty;
        public string Progress { get; set; } = string.Empty;
    }
}
