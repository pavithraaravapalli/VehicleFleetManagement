using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VehicleFleetManagement.Data;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.Services.Interfaces;
using VehicleFleetManagement.ViewModels;

namespace VehicleFleetManagement.Services.Implementations
{
    public class DriverExperienceService : IDriverExperienceService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public DriverExperienceService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<DriverDashboardViewModel?> GetDriverDashboardAsync(string userId)
        {
            var driver = await _context.Drivers
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            // Auto-heal: If UserId is not linked, match by User's email
            if (driver == null)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    driver = await _context.Drivers
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.Email.ToLower() == user.Email.ToLower());
                    if (driver != null)
                    {
                        driver.UserId = userId;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            if (driver == null) return null;

            var now = DateTime.UtcNow;
            var currentMonth = now.Month;
            var currentYear = now.Year;

            // 1. Core Trip Collections
            var trips = await _context.Trips
                .Include(t => t.Vehicle)
                .Where(t => t.DriverId == driver.DriverId)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            var completedTrips = trips.Where(t => t.TripStatus == TripStatusEnum.Completed).ToList();
            var inProgressTrip = trips.FirstOrDefault(t => t.TripStatus == TripStatusEnum.InProgress);
            var scheduledTrips = trips.Where(t => t.TripStatus == TripStatusEnum.Scheduled).OrderBy(t => t.StartDate).ToList();

            // 2. Metrics calculation
            int completedCount = completedTrips.Count;
            int inProgressCount = inProgressTrip != null ? 1 : 0;
            int upcomingCount = scheduledTrips.Count;
            decimal totalDistance = completedTrips.Sum(t => t.Distance);

            var thisMonthCompleted = completedTrips
                .Where(t => (t.ActualEndDate ?? t.StartDate).Month == currentMonth && (t.ActualEndDate ?? t.StartDate).Year == currentYear)
                .ToList();
            int currentMonthTrips = thisMonthCompleted.Count;
            decimal currentMonthDistance = thisMonthCompleted.Sum(t => t.Distance);

            // 3. Active Vehicle determination
            Vehicle? activeVehicle = inProgressTrip?.Vehicle ?? scheduledTrips.FirstOrDefault()?.Vehicle;

            // 4. Maintenance alerts for active vehicle
            var vehicleAlerts = new List<Maintenance>();
            if (activeVehicle != null)
            {
                vehicleAlerts = await _context.Maintenances
                    .Where(m => m.VehicleId == activeVehicle.VehicleId && m.Status != MaintenanceStatus.Completed)
                    .OrderBy(m => m.NextServiceDate)
                    .Take(3)
                    .ToListAsync();
            }

            // 5. Recent reported issues
            var reportedIssues = await _context.VehicleIssueReports
                .Include(i => i.Vehicle)
                .Where(i => i.DriverId == driver.DriverId)
                .OrderByDescending(i => i.ReportDate)
                .Take(4)
                .ToListAsync();

            // 6. Chart.js 6-Month Distance Telemetry
            var monthLabels = new List<string>();
            var distanceSeries = new List<decimal>();
            var tripsSeries = new List<int>();

            for (int i = 5; i >= 0; i--)
            {
                var targetDate = now.AddMonths(-i);
                var m = targetDate.Month;
                var y = targetDate.Year;
                monthLabels.Add(targetDate.ToString("MMM yyyy"));

                var monthTrips = completedTrips.Where(t => (t.ActualEndDate ?? t.StartDate).Month == m && (t.ActualEndDate ?? t.StartDate).Year == y).ToList();
                distanceSeries.Add(monthTrips.Sum(t => t.Distance));
                tripsSeries.Add(monthTrips.Count);
            }

            return new DriverDashboardViewModel
            {
                DriverId = driver.DriverId,
                DriverName = driver.FullName,
                Email = driver.Email,
                PhoneNumber = driver.PhoneNumber,
                LicenseNumber = driver.LicenseNumber,
                LicenseExpiryDate = driver.LicenseExpiryDate,
                AvailabilityStatus = driver.AvailabilityStatus,
                ProfilePictureUrl = driver.ProfilePictureUrl ?? driver.User?.ProfilePictureUrl,
                Rating = driver.Rating > 0 ? driver.Rating : 5.0m,
                TotalReviews = driver.TotalReviews,

                TotalTripsCompleted = completedCount,
                TripsInProgress = inProgressCount,
                UpcomingTripsCount = upcomingCount,
                TotalDistanceTravelled = totalDistance,
                CurrentMonthTripsCount = currentMonthTrips,
                CurrentMonthDistance = currentMonthDistance,

                ActiveVehicle = activeVehicle,
                CurrentActiveTrip = inProgressTrip,

                AssignedTrips = trips.Take(10).ToList(),
                UpcomingTimelineTrips = scheduledTrips.Take(5).ToList(),
                RecentCompletedTrips = completedTrips.Take(5).ToList(),
                ActiveVehicleMaintenanceAlerts = vehicleAlerts,
                RecentReportedIssues = reportedIssues,

                DistanceChartMonths = monthLabels,
                DistanceChartKm = distanceSeries,
                MonthlyTripsData = tripsSeries
            };
        }

        public async Task<DriverProfileViewModel?> GetDriverProfileAsync(int driverId)
        {
            var driver = await _context.Drivers
                .Include(d => d.User)
                .Include(d => d.Reviews).ThenInclude(r => r.ReviewerUser)
                .Include(d => d.Reviews).ThenInclude(r => r.Trip)
                .FirstOrDefaultAsync(d => d.DriverId == driverId);

            if (driver == null) return null;

            return await BuildDriverProfileViewModelAsync(driver);
        }

        public async Task<DriverProfileViewModel?> GetDriverProfileByUserIdAsync(string userId)
        {
            var driver = await _context.Drivers
                .Include(d => d.User)
                .Include(d => d.Reviews).ThenInclude(r => r.ReviewerUser)
                .Include(d => d.Reviews).ThenInclude(r => r.Trip)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            // Auto-heal: If UserId is not linked, match by User's email
            if (driver == null)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    driver = await _context.Drivers
                        .Include(d => d.User)
                        .Include(d => d.Reviews).ThenInclude(r => r.ReviewerUser)
                        .Include(d => d.Reviews).ThenInclude(r => r.Trip)
                        .FirstOrDefaultAsync(d => d.Email.ToLower() == user.Email.ToLower());
                    if (driver != null)
                    {
                        driver.UserId = userId;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            if (driver == null) return null;

            return await BuildDriverProfileViewModelAsync(driver);
        }

        private async Task<DriverProfileViewModel> BuildDriverProfileViewModelAsync(Driver driver)
        {
            var completedTrips = await _context.Trips
                .Where(t => t.DriverId == driver.DriverId && t.TripStatus == TripStatusEnum.Completed)
                .ToListAsync();

            var assignedVehicle = await _context.Trips
                .Include(t => t.Vehicle)
                .Where(t => t.DriverId == driver.DriverId && (t.TripStatus == TripStatusEnum.InProgress || t.TripStatus == TripStatusEnum.Scheduled))
                .OrderByDescending(t => t.StartDate)
                .Select(t => t.Vehicle)
                .FirstOrDefaultAsync();

            var visibleReviews = driver.Reviews.Where(r => !r.IsModerated).OrderByDescending(r => r.ReviewDate).ToList();

            int fiveStar = visibleReviews.Count(r => r.Rating == 5);
            int fourStar = visibleReviews.Count(r => r.Rating == 4);
            int threeStar = visibleReviews.Count(r => r.Rating == 3);
            int twoStar = visibleReviews.Count(r => r.Rating == 2);
            int oneStar = visibleReviews.Count(r => r.Rating == 1);

            var totalDistance = completedTrips.Sum(t => t.Distance);
            var completedCount = completedTrips.Count;
            var criticalFaults = await _context.VehicleIssueReports
                .CountAsync(i => i.DriverId == driver.DriverId && i.Severity == IssueSeverity.Critical);

            var achievements = new List<DriverAchievement>
            {
                new DriverAchievement
                {
                    Title = "Safe Driver",
                    Description = "Zero critical safety or mechanical defects reported during active journeys",
                    Icon = "bi-shield-check",
                    IsEarned = criticalFaults == 0,
                    BadgeClass = criticalFaults == 0 ? "bg-primary" : "bg-light text-muted border",
                    Progress = criticalFaults == 0 ? "Flawless Record" : $"{criticalFaults} issues reported"
                },
                new DriverAchievement
                {
                    Title = "Road Veteran",
                    Description = "Completed 500+ kilometers of recorded fleet transit",
                    Icon = "bi-speedometer2",
                    IsEarned = totalDistance >= 500,
                    BadgeClass = totalDistance >= 500 ? "bg-primary" : "bg-light text-muted border",
                    Progress = $"{totalDistance:N0} / 500 km"
                },
                new DriverAchievement
                {
                    Title = "5-Star Elite",
                    Description = "Maintained an exceptional customer rating of 4.8 or higher with verified reviews",
                    Icon = "bi-star-fill",
                    IsEarned = driver.Rating >= 4.8m && visibleReviews.Any(),
                    BadgeClass = (driver.Rating >= 4.8m && visibleReviews.Any()) ? "bg-warning text-dark" : "bg-light text-muted border",
                    Progress = $"{driver.Rating:F1} ★ ({visibleReviews.Count} reviews)"
                },
                new DriverAchievement
                {
                    Title = "Trip Master",
                    Description = "Successfully executed 3 or more official fleet journeys",
                    Icon = "bi-trophy-fill",
                    IsEarned = completedCount >= 3,
                    BadgeClass = completedCount >= 3 ? "bg-primary" : "bg-light text-muted border",
                    Progress = $"{completedCount} / 3 journeys"
                },
                new DriverAchievement
                {
                    Title = "Compliant Professional",
                    Description = "Commercial driving credentials verified and valid for over 6 months",
                    Icon = "bi-patch-check-fill",
                    IsEarned = driver.LicenseExpiryDate > DateTime.UtcNow.AddMonths(6),
                    BadgeClass = driver.LicenseExpiryDate > DateTime.UtcNow.AddMonths(6) ? "bg-primary" : "bg-light text-muted border",
                    Progress = $"Expires {driver.LicenseExpiryDate:MMM yyyy}"
                }
            };

            return new DriverProfileViewModel
            {
                DriverId = driver.DriverId,
                UserId = driver.UserId,
                FullName = driver.FullName,
                Email = driver.Email,
                PhoneNumber = driver.PhoneNumber,
                Address = driver.Address,
                LicenseNumber = driver.LicenseNumber,
                LicenseExpiryDate = driver.LicenseExpiryDate,
                JoiningDate = driver.JoiningDate,
                AvailabilityStatus = driver.AvailabilityStatus,
                ProfilePictureUrl = driver.ProfilePictureUrl ?? driver.User?.ProfilePictureUrl,
                Rating = driver.Rating > 0 ? driver.Rating : 5.0m,
                TotalReviews = visibleReviews.Count,
                CompletedTripsCount = completedCount,
                TotalDistanceTravelled = totalDistance,
                CurrentlyAssignedVehicle = assignedVehicle,

                FiveStarCount = fiveStar,
                FourStarCount = fourStar,
                ThreeStarCount = threeStar,
                TwoStarCount = twoStar,
                OneStarCount = oneStar,
                Reviews = visibleReviews,
                Achievements = achievements
            };
        }

        public async Task<(bool Success, string Message)> UpdateDriverProfileAsync(DriverProfileViewModel model, IFormFile? photoUpload, string webRootPath)
        {
            var driver = await _context.Drivers
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DriverId == model.DriverId);

            if (driver == null)
            {
                return (false, "Driver record not found.");
            }

            // Handle Photo Upload
            if (photoUpload != null && photoUpload.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(photoUpload.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(ext))
                {
                    return (false, "Invalid image format. Allowed formats: JPG, JPEG, PNG, WEBP.");
                }

                if (photoUpload.Length > 3 * 1024 * 1024)
                {
                    return (false, "Profile picture size exceeds 3MB limit.");
                }

                var uploadsFolder = Path.Combine(webRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"driver_{driver.DriverId}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await photoUpload.CopyToAsync(fileStream);
                }

                // Delete old photo if existed
                if (!string.IsNullOrEmpty(driver.ProfilePictureUrl))
                {
                    var oldFilePath = Path.Combine(webRootPath, driver.ProfilePictureUrl.TrimStart('/'));
                    if (File.Exists(oldFilePath))
                    {
                        try { File.Delete(oldFilePath); } catch { }
                    }
                }

                driver.ProfilePictureUrl = $"/uploads/profiles/{fileName}";
                if (driver.User != null)
                {
                    driver.User.ProfilePictureUrl = driver.ProfilePictureUrl;
                }
            }

            // Update personal details
            driver.FullName = model.FullName;
            driver.PhoneNumber = model.PhoneNumber;
            driver.Address = model.Address;

            if (driver.User != null)
            {
                driver.User.FullName = model.FullName;
                driver.User.PhoneNumber = model.PhoneNumber;
            }

            await _context.SaveChangesAsync();
            return (true, "Profile updated successfully.");
        }

        public async Task<(bool Success, string Message)> RemoveProfilePictureAsync(int driverId, string webRootPath)
        {
            var driver = await _context.Drivers
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DriverId == driverId);

            if (driver == null) return (false, "Driver not found.");

            if (!string.IsNullOrEmpty(driver.ProfilePictureUrl))
            {
                var oldFilePath = Path.Combine(webRootPath, driver.ProfilePictureUrl.TrimStart('/'));
                if (File.Exists(oldFilePath))
                {
                    try { File.Delete(oldFilePath); } catch { }
                }
            }

            driver.ProfilePictureUrl = null;
            if (driver.User != null)
            {
                driver.User.ProfilePictureUrl = null;
            }

            await _context.SaveChangesAsync();
            return (true, "Profile picture removed successfully.");
        }

        public async Task<(bool Success, string Message)> UpdateAvailabilityStatusAsync(int driverId, string newStatus, string? reason = null)
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null) return (false, "Driver not found.");

            // Rule: Cannot switch availability while an active trip is InProgress
            bool hasActiveTrip = await _context.Trips
                .AnyAsync(t => t.DriverId == driverId && t.TripStatus == TripStatusEnum.InProgress);

            if (hasActiveTrip && (newStatus == DriverStatus.Available || newStatus == DriverStatus.OnLeave || newStatus == DriverStatus.Unavailable))
            {
                return (false, "Cannot modify availability status while you are operating an active in-progress trip. Conclude or cancel the trip first.");
            }

            var previousStatus = driver.AvailabilityStatus;
            driver.AvailabilityStatus = newStatus;

            // Record audit log
            _context.DriverAvailabilityLogs.Add(new DriverAvailabilityLog
            {
                DriverId = driverId,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow,
                Reason = reason ?? $"Driver status updated to '{newStatus}' via Portal"
            });

            await _context.SaveChangesAsync();
            return (true, $"Availability status successfully updated to {newStatus}.");
        }

        public async Task<(bool Success, string Message, string? PhotoUrl)> ReportVehicleIssueAsync(CreateVehicleIssueViewModel model, string webRootPath)
        {
            var vehicle = await _context.Vehicles.FindAsync(model.VehicleId);
            if (vehicle == null) return (false, "Vehicle not found.", null);

            var driver = await _context.Drivers.FindAsync(model.DriverId);
            if (driver == null) return (false, "Driver record not found.", null);

            string? photoUrl = null;

            // Handle optional photo upload
            if (model.PhotoEvidence != null && model.PhotoEvidence.Length > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(model.PhotoEvidence.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                {
                    return (false, "Invalid photo format. Allowed formats: JPG, JPEG, PNG, WEBP.", null);
                }

                if (model.PhotoEvidence.Length > 5 * 1024 * 1024)
                {
                    return (false, "Fault photo exceeds 5MB limit.", null);
                }

                var issuesFolder = Path.Combine(webRootPath, "uploads", "issues");
                if (!Directory.Exists(issuesFolder))
                {
                    Directory.CreateDirectory(issuesFolder);
                }

                var fileName = $"issue_{vehicle.VehicleId}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(issuesFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.PhotoEvidence.CopyToAsync(stream);
                }
                photoUrl = $"/uploads/issues/{fileName}";
            }

            var issue = new VehicleIssueReport
            {
                VehicleId = model.VehicleId,
                DriverId = model.DriverId,
                TripId = model.TripId,
                Title = model.Title,
                Description = model.Description,
                Category = model.Category,
                Severity = model.Severity,
                PhotoUrl = photoUrl,
                ReportDate = DateTime.UtcNow,
                Status = IssueStatus.Open
            };

            _context.VehicleIssueReports.Add(issue);
            await _context.SaveChangesAsync();

            // Alert Administrators
            await _notificationService.NotifyAdminsAsync(
                $"Vehicle Fault Reported: {vehicle.RegistrationNumber}",
                $"Driver {driver.FullName} reported a [{model.Severity}] {model.Category} fault on {vehicle.VehicleName}: '{model.Title}'",
                NotificationType.VehicleIssueReported,
                $"/Issues/Details/{issue.IssueReportId}"
            );

            return (true, "Vehicle fault reported successfully. Maintenance team has been notified.", photoUrl);
        }

        public async Task<(bool Success, string Message)> UpdateVehicleIssueStatusAsync(int issueReportId, string status, string? resolutionNotes)
        {
            var issue = await _context.VehicleIssueReports
                .Include(i => i.Driver)
                .Include(i => i.Vehicle)
                .FirstOrDefaultAsync(i => i.IssueReportId == issueReportId);

            if (issue == null) return (false, "Fault report not found.");

            issue.Status = status;
            issue.ResolutionNotes = resolutionNotes;
            if (status == IssueStatus.Resolved || status == IssueStatus.Closed)
            {
                issue.ResolvedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Notify Driver
            if (issue.Driver != null && !string.IsNullOrEmpty(issue.Driver.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    issue.Driver.UserId,
                    $"Vehicle Fault Update: {issue.Vehicle?.RegistrationNumber}",
                    $"Your fault report '{issue.Title}' has been updated to [{status}]. Notes: {resolutionNotes ?? "No additional notes."}",
                    NotificationType.VehicleIssueUpdate,
                    $"/Issues/Details/{issue.IssueReportId}"
                );
            }

            return (true, $"Fault report updated to {status}.");
        }

        public async Task<List<VehicleIssueReport>> GetDriverIssuesAsync(int driverId)
        {
            return await _context.VehicleIssueReports
                .Include(i => i.Vehicle)
                .Include(i => i.Trip)
                .Where(i => i.DriverId == driverId)
                .OrderByDescending(i => i.ReportDate)
                .ToListAsync();
        }

        public async Task<List<VehicleIssueReport>> GetAllIssuesAsync(string? statusFilter = null, string? severityFilter = null, int? vehicleId = null)
        {
            var query = _context.VehicleIssueReports
                .Include(i => i.Vehicle)
                .Include(i => i.Driver)
                .Include(i => i.Trip)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(i => i.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(severityFilter))
            {
                query = query.Where(i => i.Severity == severityFilter);
            }

            if (vehicleId.HasValue && vehicleId.Value > 0)
            {
                query = query.Where(i => i.VehicleId == vehicleId.Value);
            }

            return await query.OrderByDescending(i => i.ReportDate).ToListAsync();
        }

        public async Task<VehicleIssueReport?> GetIssueByIdAsync(int issueReportId)
        {
            return await _context.VehicleIssueReports
                .Include(i => i.Vehicle)
                .Include(i => i.Driver)
                .Include(i => i.Trip)
                .FirstOrDefaultAsync(i => i.IssueReportId == issueReportId);
        }

        public async Task<(bool Success, string Message)> SubmitDriverReviewAsync(SubmitDriverReviewViewModel model, string reviewerUserId)
        {
            // Verify trip is completed
            var trip = await _context.Trips
                .Include(t => t.Driver)
                .FirstOrDefaultAsync(t => t.TripId == model.TripId);

            if (trip == null) return (false, "Trip not found.");

            if (trip.TripStatus != TripStatusEnum.Completed)
            {
                return (false, "Only completed trips can be reviewed.");
            }

            if (trip.DriverId != model.DriverId)
            {
                return (false, "Assigned driver mismatch.");
            }

            // Prevent duplicate review for the same trip and reviewer
            bool alreadyReviewed = await _context.DriverReviews
                .AnyAsync(r => r.TripId == model.TripId && r.ReviewerUserId == reviewerUserId);

            if (alreadyReviewed)
            {
                return (false, "You have already submitted a review for this trip.");
            }

            var review = new DriverReview
            {
                DriverId = model.DriverId,
                TripId = model.TripId,
                ReviewerUserId = reviewerUserId,
                Rating = model.Rating,
                Comment = model.Comment,
                ReviewDate = DateTime.UtcNow,
                IsModerated = false
            };

            _context.DriverReviews.Add(review);
            await _context.SaveChangesAsync();

            // Recalculate Driver Average Rating and Total Review Count
            var allReviews = await _context.DriverReviews
                .Where(r => r.DriverId == model.DriverId && !r.IsModerated)
                .ToListAsync();

            if (trip.Driver != null)
            {
                trip.Driver.TotalReviews = allReviews.Count;
                trip.Driver.Rating = allReviews.Any() ? Math.Round((decimal)allReviews.Average(r => r.Rating), 2) : 5.0m;
                await _context.SaveChangesAsync();

                // Send notification to the driver
                if (!string.IsNullOrEmpty(trip.Driver.UserId))
                {
                    await _notificationService.CreateNotificationAsync(
                        trip.Driver.UserId,
                        "New Performance Review Received",
                        $"You received a {model.Rating}-star review for trip #{trip.TripId}: \"{model.Comment}\"",
                        NotificationType.General,
                        "/Account/Profile"
                    );
                }
            }

            return (true, "Review submitted successfully.");
        }

        public async Task<List<DriverReview>> GetDriverReviewsAsync(int driverId, bool includeModerated = false)
        {
            var query = _context.DriverReviews
                .Include(r => r.ReviewerUser)
                .Include(r => r.Trip)
                .Where(r => r.DriverId == driverId);

            if (!includeModerated)
            {
                query = query.Where(r => !r.IsModerated);
            }

            return await query.OrderByDescending(r => r.ReviewDate).ToListAsync();
        }

        public async Task<(bool Success, string Message)> ModerateReviewAsync(int reviewId, bool hideReview)
        {
            var review = await _context.DriverReviews
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (review == null) return (false, "Review not found.");

            review.IsModerated = hideReview;
            await _context.SaveChangesAsync();

            // Recalculate Driver Average
            if (review.Driver != null)
            {
                var activeReviews = await _context.DriverReviews
                    .Where(r => r.DriverId == review.DriverId && !r.IsModerated)
                    .ToListAsync();

                review.Driver.TotalReviews = activeReviews.Count;
                review.Driver.Rating = activeReviews.Any() ? Math.Round((decimal)activeReviews.Average(r => r.Rating), 2) : 5.0m;
                await _context.SaveChangesAsync();
            }

            return (true, hideReview ? "Review has been hidden from public view." : "Review restored.");
        }
    }
}
