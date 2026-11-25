using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VehicleFleetManagement.ViewModels
{
    /// <summary>
    /// ViewModel for managing normal user personal profile information,
    /// avatar uploads, contact details, and account security.
    /// </summary>
    public class UserProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Current Profile Picture")]
        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "Account Role")]
        public string RoleName { get; set; } = "User";

        [Display(Name = "Account Created On")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Total Driver Reviews Submitted")]
        public int ReviewsCount { get; set; }

        [Display(Name = "Upload New Profile Picture")]
        public IFormFile? ProfilePicture { get; set; }

        // Change Password sub-form
        public ChangePasswordViewModel ChangePassword { get; set; } = new ChangePasswordViewModel();
    }

    /// <summary>
    /// ViewModel for self-service user password updates.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation password do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
