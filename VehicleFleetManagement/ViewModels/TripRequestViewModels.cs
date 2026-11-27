using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.ViewModels
{
    public class CreateTripRequestViewModel
    {
        [Required(ErrorMessage = "Origin / Pickup location is required.")]
        [StringLength(100, ErrorMessage = "Origin cannot exceed 100 characters.")]
        [Display(Name = "Origin / Pickup Location")]
        public string Source { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination location is required.")]
        [StringLength(100, ErrorMessage = "Destination cannot exceed 100 characters.")]
        [Display(Name = "Destination Location")]
        public string Destination { get; set; } = string.Empty;

        [Required(ErrorMessage = "Scheduled departure date and time are required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Scheduled Departure")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddHours(2);

        [Required(ErrorMessage = "Estimated return date and time are required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Estimated Return")]
        public DateTime EstimatedEndDate { get; set; } = DateTime.UtcNow.AddHours(6);

        [Required(ErrorMessage = "Please specify number of passengers.")]
        [Range(1, 100, ErrorMessage = "Passenger count must be between 1 and 100.")]
        [Display(Name = "Passenger Count")]
        public int PassengerCount { get; set; } = 1;

        [StringLength(50)]
        [Display(Name = "Preferred Vehicle Type")]
        public string? PreferredVehicleType { get; set; } = "Any";

        [Required(ErrorMessage = "Official purpose or justification is required.")]
        [StringLength(500, ErrorMessage = "Purpose cannot exceed 500 characters.")]
        [Display(Name = "Official Purpose / Justification")]
        public string Purpose { get; set; } = string.Empty;
    }

    public class ApproveTripRequestViewModel
    {
        public int TripRequestId { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public string RequesterEmail { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EstimatedEndDate { get; set; }
        public int PassengerCount { get; set; }
        public string? PreferredVehicleType { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        [Required(ErrorMessage = "Please select an available vehicle.")]
        [Display(Name = "Assign Vehicle")]
        public int SelectedVehicleId { get; set; }

        [Required(ErrorMessage = "Please select an available driver.")]
        [Display(Name = "Assign Driver")]
        public int SelectedDriverId { get; set; }

        [StringLength(500)]
        [Display(Name = "Admin Approval Remarks / Dispatch Notes")]
        public string? AdminNotes { get; set; }

        public IEnumerable<SelectListItem> AvailableVehicles { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> AvailableDrivers { get; set; } = new List<SelectListItem>();
    }

    public class RejectTripRequestViewModel
    {
        public int TripRequestId { get; set; }

        [Required(ErrorMessage = "Rejection reason is required to notify requester.")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        [Display(Name = "Rejection Reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
