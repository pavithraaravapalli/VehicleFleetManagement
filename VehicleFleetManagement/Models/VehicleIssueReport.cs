using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleFleetManagement.Models
{
    /// <summary>
    /// Represents a mechanical or operational vehicle fault reported by a driver.
    /// Categorized by severity (Low, Medium, High, Critical) with optional photographic evidence.
    /// </summary>
    public class VehicleIssueReport
    {
        [Key]
        [Display(Name = "Issue Report ID")]
        public int IssueReportId { get; set; }

        [Required(ErrorMessage = "Please select the vehicle.")]
        [ForeignKey("Vehicle")]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }

        [Required]
        [ForeignKey("Driver")]
        [Display(Name = "Reporting Driver")]
        public int DriverId { get; set; }

        [ForeignKey("Trip")]
        [Display(Name = "Associated Trip")]
        public int? TripId { get; set; }

        [Required(ErrorMessage = "Issue title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        [Display(Name = "Issue Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Detailed issue description is required.")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        [Display(Name = "Issue Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an issue category.")]
        [StringLength(50)]
        [Display(Name = "Fault Category")]
        public string Category { get; set; } = IssueCategory.Engine; // Engine, Tyres, Brakes, Battery, Fuel, Other

        [Required(ErrorMessage = "Please specify issue severity.")]
        [StringLength(20)]
        [Display(Name = "Severity Level")]
        public string Severity { get; set; } = IssueSeverity.Medium; // Low, Medium, High, Critical

        [StringLength(255)]
        [Display(Name = "Photo Evidence Path")]
        public string? PhotoUrl { get; set; }

        [Display(Name = "Date Reported")]
        public DateTime ReportDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(25)]
        [Display(Name = "Issue Status")]
        public string Status { get; set; } = IssueStatus.Open; // Open, UnderReview, Resolved, Closed

        [StringLength(500)]
        [Display(Name = "Admin Resolution Notes")]
        public string? ResolutionNotes { get; set; }

        [Display(Name = "Resolved At")]
        public DateTime? ResolvedAt { get; set; }

        // Navigation Properties
        public virtual Vehicle? Vehicle { get; set; }
        public virtual Driver? Driver { get; set; }
        public virtual Trip? Trip { get; set; }
    }

    public static class IssueCategory
    {
        public const string Engine = "Engine";
        public const string Tyres = "Tyres";
        public const string Brakes = "Brakes";
        public const string Battery = "Battery";
        public const string Fuel = "Fuel";
        public const string Other = "Other";
    }

    public static class IssueSeverity
    {
        public const string Low = "Low";
        public const string Medium = "Medium";
        public const string High = "High";
        public const string Critical = "Critical";
    }

    public static class IssueStatus
    {
        public const string Open = "Open";
        public const string UnderReview = "UnderReview";
        public const string Resolved = "Resolved";
        public const string Closed = "Closed";
    }
}
