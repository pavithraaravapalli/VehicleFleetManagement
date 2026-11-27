using System;
using System.Collections.Generic;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.ViewModels
{
    /// <summary>
    /// Strongly-typed ViewModel for the Normal User / Staff Transportation Dashboard.
    /// Exclusively aggregates data for the logged-in user: their bookings, assigned journeys, and alerts.
    /// </summary>
    public class UserDashboardViewModel
    {
        // 1. Current User Identity
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime MemberSince { get; set; }

        // 2. User-Specific Metrics
        public int PendingRequestsCount { get; set; }
        public int ApprovedRequestsCount { get; set; }
        public int UpcomingTripsCount { get; set; }
        public int CompletedTripsCount { get; set; }
        public int UnreadNotificationsCount { get; set; }
        public int TotalReviewsWrittenCount { get; set; }

        // 3. User-Specific Collections
        public IEnumerable<TripRequest> MyBookingRequests { get; set; } = new List<TripRequest>();
        public IEnumerable<Trip> MyUpcomingTrips { get; set; } = new List<Trip>();
        public IEnumerable<Trip> MyCompletedTrips { get; set; } = new List<Trip>();
        public IEnumerable<Notification> RecentNotifications { get; set; } = new List<Notification>();
        public IEnumerable<DriverReview> MyRecentReviews { get; set; } = new List<DriverReview>();
    }
}
