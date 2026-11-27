using System;
using System.Collections.Generic;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.ViewModels
{
    /// <summary>
    /// Comprehensive ViewModel for the Executive Admin Fleet Operations Dashboard.
    /// Aggregates key asset, driver, trip, maintenance, fuel, and visual chart data directly from SQL Server.
    /// </summary>
    public class AdminDashboardViewModel
    {
        // 1. Vehicle Fleet Status Metrics
        public int TotalVehicles { get; set; }
        public int AvailableVehicles { get; set; }
        public int AssignedVehicles { get; set; }
        public int UnderMaintenanceVehicles { get; set; }

        // 2. Driver Workforce Metrics
        public int TotalDrivers { get; set; }
        public int ActiveDrivers { get; set; }

        // 3. Trip Dispatch Lifecycle Metrics
        public int TotalTrips { get; set; }
        public int ActiveTrips { get; set; }
        public int CompletedTrips { get; set; }
        public int PendingTrips { get; set; }

        // 4. Financial & Operational Cost Metrics
        public decimal TotalFuelExpenses { get; set; }
        public decimal TotalMaintenanceExpenses { get; set; }
        public decimal TotalFleetExpenses => TotalFuelExpenses + TotalMaintenanceExpenses;

        // 5. Proactive Due Reminders
        public int UpcomingMaintenanceAlertsCount { get; set; }
        public IEnumerable<Maintenance> UpcomingMaintenanceAlerts { get; set; } = new List<Maintenance>();

        // 6. Recent Operational Activity Logs
        public IEnumerable<Trip> RecentTrips { get; set; } = new List<Trip>();
        public IEnumerable<Vehicle> RecentVehicles { get; set; } = new List<Vehicle>();

        // 7. Visual Chart Data Series (For Chart.js Integration)
        public List<string> VehicleStatusLabels { get; set; } = new();
        public List<int> VehicleStatusCounts { get; set; } = new();

        public List<string> TripStatusLabels { get; set; } = new();
        public List<int> TripStatusCounts { get; set; } = new();

        public List<string> ExpenseMonths { get; set; } = new();
        public List<decimal> MonthlyFuelData { get; set; } = new();
        public List<decimal> MonthlyMaintenanceData { get; set; } = new();

        // 8. Vehicle Issue Reporting & Driver Availability Telemetry
        public int OpenVehicleIssuesCount { get; set; }
        public int AvailableDriversCount { get; set; }
        public int OnTripDriversCount { get; set; }
        public IEnumerable<VehicleIssueReport> RecentVehicleIssues { get; set; } = new List<VehicleIssueReport>();

        // 9. Booking Requests Telemetry
        public int PendingTripRequestsCount { get; set; }
        public IEnumerable<TripRequest> RecentTripRequests { get; set; } = new List<TripRequest>();
    }
}
