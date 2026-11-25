using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleFleetManagement.Data;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.Services.Interfaces;
using VehicleFleetManagement.ViewModels;

namespace VehicleFleetManagement.Controllers
{
    /// <summary>
    /// Serves role-specific dashboards with strict authorization checks:
    /// - Admin: Full fleet operational control, visual charts, open issues, and BI reporting metrics
    /// - Driver: Dedicated driver workspace, KPI cards, timeline, monthly distance charts, and quick actions
    /// - User: Dedicated staff transportation portal, available vehicles, recent trips, notifications, and reviews
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IReportService _reportService;
        private readonly IDriverExperienceService _driverExperienceService;
        private readonly INotificationService _notificationService;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IReportService reportService,
            IDriverExperienceService driverExperienceService,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _reportService = reportService;
            _driverExperienceService = driverExperienceService;
            _notificationService = notificationService;
        }

        // ==========================================
        // GET: /Dashboard/Index (Role Router)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (await _userManager.IsInRoleAsync(user, DbSeeder.RoleAdmin))
            {
                return RedirectToAction(nameof(Admin));
            }

            if (await _userManager.IsInRoleAsync(user, DbSeeder.RoleDriver))
            {
                return RedirectToAction(nameof(Driver));
            }

            return RedirectToAction("User");
        }

        // ==========================================
        // GET: /Dashboard/Admin
        // Restrict admin pages to Admin
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        public async Task<IActionResult> Admin()
        {
            ViewBag.Title = "Executive Fleet Operations Dashboard - FleetFlow Pro";
            var dashboardData = await _reportService.GetAdminDashboardStatsAsync();

            // Additional real-time statistics
            dashboardData.OpenVehicleIssuesCount = await _context.VehicleIssueReports.CountAsync(i => i.Status == IssueStatus.Open || i.Status == IssueStatus.UnderReview);
            dashboardData.AvailableDriversCount = await _context.Drivers.CountAsync(d => d.AvailabilityStatus == DriverStatus.Available);
            dashboardData.OnTripDriversCount = await _context.Drivers.CountAsync(d => d.AvailabilityStatus == DriverStatus.OnTrip);
            dashboardData.RecentVehicleIssues = await _context.VehicleIssueReports
                .Include(i => i.Vehicle)
                .Include(i => i.Driver)
                .OrderByDescending(i => i.ReportDate)
                .Take(5)
                .ToListAsync();

            ViewBag.OpenVehicleIssuesCount = dashboardData.OpenVehicleIssuesCount;
            ViewBag.AvailableDriversCount = dashboardData.AvailableDriversCount;
            ViewBag.OnTripDriversCount = dashboardData.OnTripDriversCount;
            ViewBag.RecentIssues = dashboardData.RecentVehicleIssues;

            // Run automated proactive fleet check (maintenance & license expiries)
            try
            {
                await _notificationService.CheckAndGenerateFleetAlertsAsync();
            }
            catch { }

            return View(dashboardData);
        }

        // ==========================================
        // GET: /Dashboard/Driver
        // Dedicated, stand-out Driver Dashboard
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleDriver)]
        public async Task<IActionResult> Driver()
        {
            ViewBag.Title = "Driver Workspace - FleetFlow Pro";

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var driverDashboard = await _driverExperienceService.GetDriverDashboardAsync(user.Id);
            if (driverDashboard == null)
            {
                ViewBag.ErrorMessage = "No driver profile has been associated with your account yet. Please contact your Fleet Administrator.";
                return View("DriverNotFound");
            }

            return View(driverDashboard);
        }

        // ==========================================
        // GET: /Dashboard/User
        // Dedicated Normal User / Staff Transportation Workspace
        // ==========================================
        [HttpGet]
        [Authorize(Roles = DbSeeder.RoleUser)]
        [ActionName("User")]
        public async Task<IActionResult> UserDashboard()
        {
            ViewBag.Title = "Staff Workspace - FleetFlow Pro";

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new UserDashboardViewModel
            {
                UserId = user.Id,
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? (user.UserName ?? "User") : user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl,
                MemberSince = user.CreatedAt,
                AvailableVehiclesCount = await _context.Vehicles.CountAsync(v => v.Status == VehicleStatus.Available),
                ActiveTripsCount = await _context.Trips.CountAsync(t => t.TripStatus == TripStatusEnum.Scheduled || t.TripStatus == TripStatusEnum.InProgress),
                UnreadNotificationsCount = await _context.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead),
                TotalReviewsWrittenCount = await _context.DriverReviews.CountAsync(r => r.ReviewerUserId == user.Id),
                AvailableVehicles = await _context.Vehicles
                    .Where(v => v.Status == VehicleStatus.Available)
                    .OrderBy(v => v.VehicleName)
                    .Take(6)
                    .ToListAsync(),
                RecentTrips = await _context.Trips
                    .Include(t => t.Vehicle)
                    .Include(t => t.Driver)
                    .OrderByDescending(t => t.StartDate)
                    .Take(5)
                    .ToListAsync(),
                RecentNotifications = await _context.Notifications
                    .Where(n => n.UserId == user.Id)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                MyRecentReviews = await _context.DriverReviews
                    .Include(r => r.Driver)
                    .Include(r => r.Trip)
                    .Where(r => r.ReviewerUserId == user.Id)
                    .OrderByDescending(r => r.ReviewDate)
                    .Take(4)
                    .ToListAsync()
            };

            return View(viewModel);
        }
    }
}
