using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.ViewModels;

namespace VehicleFleetManagement.Services.Interfaces
{
    public interface IDriverExperienceService
    {
        Task<DriverDashboardViewModel?> GetDriverDashboardAsync(string userId);
        Task<DriverProfileViewModel?> GetDriverProfileAsync(int driverId);
        Task<DriverProfileViewModel?> GetDriverProfileByUserIdAsync(string userId);
        Task<(bool Success, string Message)> UpdateDriverProfileAsync(DriverProfileViewModel model, IFormFile? photoUpload, string webRootPath);
        Task<(bool Success, string Message)> RemoveProfilePictureAsync(int driverId, string webRootPath);
        Task<(bool Success, string Message)> UpdateAvailabilityStatusAsync(int driverId, string newStatus, string? reason = null);
        Task<(bool Success, string Message, string? PhotoUrl)> ReportVehicleIssueAsync(CreateVehicleIssueViewModel model, string webRootPath);
        Task<(bool Success, string Message)> UpdateVehicleIssueStatusAsync(int issueReportId, string status, string? resolutionNotes);
        Task<List<VehicleIssueReport>> GetDriverIssuesAsync(int driverId);
        Task<List<VehicleIssueReport>> GetAllIssuesAsync(string? statusFilter = null, string? severityFilter = null, int? vehicleId = null);
        Task<VehicleIssueReport?> GetIssueByIdAsync(int issueReportId);
        Task<(bool Success, string Message)> SubmitDriverReviewAsync(SubmitDriverReviewViewModel model, string reviewerUserId);
        Task<List<DriverReview>> GetDriverReviewsAsync(int driverId, bool includeModerated = false);
        Task<(bool Success, string Message)> ModerateReviewAsync(int reviewId, bool hideReview);
    }
}
