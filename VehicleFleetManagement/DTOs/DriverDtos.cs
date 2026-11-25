using System;
using System.ComponentModel.DataAnnotations;

namespace VehicleFleetManagement.DTOs
{
    /// <summary>
    /// Safe public representation of driver profile data.
    /// Does NOT expose underlying Identity User IDs, security tokens, or password hashes.
    /// </summary>
    public class DriverDto
    {
        public int DriverId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime LicenseExpiryDate { get; set; }
        public string Address { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }
        public string AvailabilityStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Input DTO for registering a new driver.
    /// </summary>
    public class CreateDriverDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "License number is required.")]
        [StringLength(50, ErrorMessage = "License number cannot exceed 50 characters.")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "License expiry date is required.")]
        public DateTime LicenseExpiryDate { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Joining date is required.")]
        public DateTime JoiningDate { get; set; }
    }

    /// <summary>
    /// Input DTO for updating an existing driver's profile.
    /// </summary>
    public class UpdateDriverDto
    {
        [Required]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "License number is required.")]
        [StringLength(50, ErrorMessage = "License number cannot exceed 50 characters.")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "License expiry date is required.")]
        public DateTime LicenseExpiryDate { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Joining date is required.")]
        public DateTime JoiningDate { get; set; }

        [Required(ErrorMessage = "Availability status is required.")]
        public string AvailabilityStatus { get; set; } = string.Empty; // Available, OnTrip, Inactive
    }
}
