using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.ViewModels
{
    /// <summary>
    /// ViewModel for scheduling and logging vehicle maintenance records.
    /// </summary>
    public class CreateMaintenanceViewModel
    {
        public int MaintenanceId { get; set; }

        [Required(ErrorMessage = "Please select a vehicle.")]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Service type is required.")]
        [StringLength(50, ErrorMessage = "Service type cannot exceed 50 characters.")]
        [Display(Name = "Service Type")]
        public string ServiceType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "Service Description / Notes")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Service date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Service Date")]
        public DateTime ServiceDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Next scheduled service date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Next Service Due Date")]
        public DateTime NextServiceDate { get; set; } = DateTime.UtcNow.AddMonths(6);

        [Required(ErrorMessage = "Service cost is required.")]
        [Range(0, 10000000, ErrorMessage = "Cost must be a positive value.")]
        [Display(Name = "Total Service Cost ($)")]
        public decimal Cost { get; set; }

        [Required(ErrorMessage = "Service center name is required.")]
        [StringLength(150, ErrorMessage = "Service center cannot exceed 150 characters.")]
        [Display(Name = "Service Center / Garage Name")]
        public string ServiceCenter { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Maintenance Status")]
        public string Status { get; set; } = MaintenanceStatus.Scheduled;

        public IEnumerable<SelectListItem>? AvailableVehicles { get; set; }
    }

    /// <summary>
    /// Dashboard index ViewModel displaying maintenance records and proactive alerts.
    /// </summary>
    public class MaintenanceIndexViewModel
    {
        public IEnumerable<Maintenance> Maintenances { get; set; } = new List<Maintenance>();
        public IEnumerable<Maintenance> UpcomingAlerts { get; set; } = new List<Maintenance>();
        public IEnumerable<Maintenance> OverdueAlerts { get; set; } = new List<Maintenance>();
        public decimal TotalMaintenanceCost { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
        public string? SelectedStatus { get; set; }
        public int? SelectedVehicleId { get; set; }
    }
}
