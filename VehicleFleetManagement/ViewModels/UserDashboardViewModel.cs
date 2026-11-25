using System;
using System.Collections.Generic;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.ViewModels
{
    /// <summary>
    /// Strongly-typed ViewModel for the Normal User / Staff Transportation Dashboard.
    /// Aggregates real data directly from SQL Server for the logged-in user.
    /// </summary>
    public class UserDashboardViewModel
    {
        // 1. Current User Details
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime MemberSince { get; set; }

        // 2. Real SQL Server KPI Counts
        public int AvailableVehiclesCount { get; set; }
        public int ActiveTripsCount { get; set; }
        public int UnreadNotificationsCount { get; set; }
        public int TotalReviewsWrittenCount { get; set; }

        // 3. Operational Real Data Collections
        public IEnumerable<Vehicle> AvailableVehicles { get; set; } = new List<Vehicle>();
        public IEnumerable<Trip> RecentTrips { get; set; } = new List<Trip>();
        public IEnumerable<Notification> RecentNotifications { get; set; } = new List<Notification>();
        public IEnumerable<DriverReview> MyRecentReviews { get; set; } = new List<DriverReview>();
    }
}
