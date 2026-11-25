using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleFleetManagement.Models
{
    /// <summary>
    /// Represents passenger or administrator feedback and rating submitted for an eligible completed trip.
    /// Drives the 5-star driver performance rating system.
    /// </summary>
    public class DriverReview
    {
        [Key]
        [Display(Name = "Review ID")]
        public int ReviewId { get; set; }

        [Required]
        [ForeignKey("Driver")]
        [Display(Name = "Driver")]
        public int DriverId { get; set; }

        [Required]
        [ForeignKey("Trip")]
        [Display(Name = "Completed Trip")]
        public int TripId { get; set; }

        [Required]
        [ForeignKey("ReviewerUser")]
        [Display(Name = "Reviewer User")]
        public string ReviewerUserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a star rating between 1 and 5.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        [Display(Name = "Star Rating")]
        public int Rating { get; set; } = 5;

        [Required(ErrorMessage = "Please provide feedback comments.")]
        [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters.")]
        [Display(Name = "Review Comments")]
        public string Comment { get; set; } = string.Empty;

        [Display(Name = "Review Date")]
        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Moderated / Hidden by Admin")]
        public bool IsModerated { get; set; } = false;

        // Navigation Properties
        public virtual Driver? Driver { get; set; }
        public virtual Trip? Trip { get; set; }
        public virtual ApplicationUser? ReviewerUser { get; set; }
    }
}
