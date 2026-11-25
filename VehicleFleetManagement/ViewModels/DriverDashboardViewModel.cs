using System;
using System.Collections.Generic;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.ViewModels
{
    /// <summary>
    /// Telemetry and UI view model for the dedicated Driver Experience Dashboard.
    /// Aggregates real-time trips, assigned vehicle health, ratings, and monthly distance stats.
    /// </summary>
    public class DriverDashboardViewModel
    {
        // Driver Identity & Status
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime LicenseExpiryDate { get; set; }
        public string AvailabilityStatus { get; set; } = DriverStatus.Available;
        public string? ProfilePictureUrl { get; set; }
        public decimal Rating { get; set; } = 5.0m;
        public int TotalReviews { get; set; }

        // Statistical KPI Metrics (Retrieved from SQL Server)
        public int TotalTripsCompleted { get; set; }
        public int TripsInProgress { get; set; }
        public int UpcomingTripsCount { get; set; }
        public decimal TotalDistanceTravelled { get; set; }
        public int CurrentMonthTripsCount { get; set; }
        public decimal CurrentMonthDistance { get; set; }

        // Assigned Vehicle Details (if currently assigned or in-progress)
        public Vehicle? ActiveVehicle { get; set; }
        public Trip? CurrentActiveTrip { get; set; }

        // Collections for Interactive Views
        public List<Trip> AssignedTrips { get; set; } = new List<Trip>();
        public List<Trip> UpcomingTimelineTrips { get; set; } = new List<Trip>();
        public List<Trip> RecentCompletedTrips { get; set; } = new List<Trip>();
        public List<Maintenance> ActiveVehicleMaintenanceAlerts { get; set; } = new List<Maintenance>();
        public List<VehicleIssueReport> RecentReportedIssues { get; set; } = new List<VehicleIssueReport>();

        // Chart.js Monthly Travel Visualization (Last 6 Months)
        public List<string> DistanceChartMonths { get; set; } = new List<string>();
        public List<decimal> DistanceChartKm { get; set; } = new List<decimal>();
        public List<int> MonthlyTripsData { get; set; } = new List<int>();
    }
}
