using System;
using System.ComponentModel.DataAnnotations;

namespace VehicleFleetManagement.ViewModels
{
    public class SubmitDriverReviewViewModel
    {
        [Required]
        public int DriverId { get; set; }

        [Required]
        public int TripId { get; set; }

        public string DriverName { get; set; } = string.Empty;
        public string? DriverPhotoUrl { get; set; }
        public string TripDetails { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a star rating between 1 and 5.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        [Display(Name = "Rating (1 to 5 Stars)")]
        public int Rating { get; set; } = 5;

        [Required(ErrorMessage = "Please enter your review feedback.")]
        [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters.")]
        [Display(Name = "Feedback Comments")]
        public string Comment { get; set; } = string.Empty;
    }
}
