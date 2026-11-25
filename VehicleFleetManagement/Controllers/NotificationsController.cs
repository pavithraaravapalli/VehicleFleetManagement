using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleFleetManagement.Services.Interfaces;

namespace VehicleFleetManagement.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // ==========================================
        // GET: /Notifications
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 50);
            ViewBag.UnreadCount = await _notificationService.GetUnreadCountAsync(userId);
            ViewBag.Title = "Notification Center - FleetFlow Pro";

            return View(notifications);
        }

        // ==========================================
        // POST: /Notifications/MarkAsRead/5
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await _notificationService.MarkAsReadAsync(id, userId);
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // POST: /Notifications/MarkAllAsRead
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await _notificationService.MarkAllAsReadAsync(userId);
                TempData["Success"] = "All notifications marked as read.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // GET: /Notifications/GetUnreadCount (JSON AJAX)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Json(new { count = 0 });

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Json(new { count });
        }
    }
}
