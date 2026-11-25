using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.ViewModels
{
    public class CreateVehicleIssueViewModel
    {
        [Required(ErrorMessage = "Please select the vehicle.")]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }

        public int DriverId { get; set; }

        [Display(Name = "Associated Trip (Optional)")]
        public int? TripId { get; set; }

        [Required(ErrorMessage = "Please enter an issue title.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        [Display(Name = "Fault Summary / Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please describe the problem in detail.")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        [Display(Name = "Detailed Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select the fault category.")]
        [Display(Name = "Issue Category")]
        public string Category { get; set; } = IssueCategory.Engine;

        [Required(ErrorMessage = "Please select the severity level.")]
        [Display(Name = "Severity")]
        public string Severity { get; set; } = IssueSeverity.Medium;

        [Display(Name = "Upload Photo Evidence (Optional)")]
        public IFormFile? PhotoEvidence { get; set; }

        // Contextual Display
        public string? VehicleRegistration { get; set; }
        public string? VehicleName { get; set; }
    }

    public class UpdateVehicleIssueViewModel
    {
        public int IssueReportId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public DateTime ReportDate { get; set; }

        public string VehicleName { get; set; } = string.Empty;
        public string VehicleRegistration { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select the current status.")]
        [Display(Name = "Update Status")]
        public string Status { get; set; } = IssueStatus.UnderReview;

        [StringLength(500, ErrorMessage = "Resolution notes cannot exceed 500 characters.")]
        [Display(Name = "Resolution / Workshop Notes")]
        public string? ResolutionNotes { get; set; }
    }

    public class VehicleIssueFilterViewModel
    {
        public string? StatusFilter { get; set; }
        public string? SeverityFilter { get; set; }
        public int? VehicleId { get; set; }
        public List<VehicleIssueReport> Issues { get; set; } = new List<VehicleIssueReport>();
    }
}
