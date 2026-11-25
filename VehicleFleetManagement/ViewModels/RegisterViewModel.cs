using System.ComponentModel.DataAnnotations;

namespace VehicleFleetManagement.ViewModels
{
    /// <summary>
    /// ViewModel for self-service public user and driver registration.
    /// Strictly limits self-assigned roles to User or Driver (Admin cannot be chosen).
    /// </summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your primary account role.")]
        [Display(Name = "Account Type")]
        public string Role { get; set; } = "User"; // "User" or "Driver" (Strictly no "Admin")

        // Driver-specific onboarding fields (optional if Driver role selected)
        [Display(Name = "Driving License Number")]
        [StringLength(30, ErrorMessage = "License number cannot exceed 30 characters.")]
        public string? LicenseNumber { get; set; }

        [Display(Name = "Residential Address")]
        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        public string? Address { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the Terms and Conditions to proceed.")]
        [Display(Name = "I agree to the Terms of Service & Privacy Policy")]
        public bool AgreeToTerms { get; set; }
    }
}
