using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VehicleFleetManagement.Data;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.Services.Interfaces;

namespace VehicleFleetManagement.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return 0;
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 20)
        {
            if (string.IsNullOrEmpty(userId)) return new List<Notification>();
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null) return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!unread.Any()) return true;

            foreach (var item in unread)
            {
                item.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Notification> CreateNotificationAsync(string userId, string title, string message, string type, string? actionUrl = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task NotifyAdminsAsync(string title, string message, string type, string? actionUrl = null)
        {
            var admins = await _userManager.GetUsersInRoleAsync(DbSeeder.RoleAdmin);
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    ActionUrl = actionUrl,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }
            await _context.SaveChangesAsync();
        }

        public async Task CheckAndGenerateFleetAlertsAsync()
        {
            var now = DateTime.UtcNow;
            var sevenDaysAhead = now.AddDays(7);

            // 1. Check upcoming maintenance
            var upcomingMaint = await _context.Maintenances
                .Include(m => m.Vehicle)
                .Where(m => m.Status == MaintenanceStatus.Scheduled && m.NextServiceDate <= sevenDaysAhead && m.NextServiceDate >= now)
                .ToListAsync();

            var admins = await _userManager.GetUsersInRoleAsync(DbSeeder.RoleAdmin);

            foreach (var maint in upcomingMaint)
            {
                var title = $"Maintenance Reminder: {maint.Vehicle?.RegistrationNumber}";
                bool alreadySent = await _context.Notifications.AnyAsync(n => n.Title == title && n.CreatedAt >= now.AddDays(-3));
                if (!alreadySent)
                {
                    foreach (var admin in admins)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = admin.Id,
                            Title = title,
                            Message = $"Vehicle {maint.Vehicle?.VehicleName} ({maint.Vehicle?.RegistrationNumber}) has {maint.ServiceType} due on {maint.NextServiceDate:MMM dd, yyyy}.",
                            Type = NotificationType.MaintenanceDue,
                            ActionUrl = $"/Maintenance/Details/{maint.MaintenanceId}",
                            CreatedAt = now
                        });
                    }
                }
            }

            // 2. Check driver license expiry within 30 days
            var thirtyDaysAhead = now.AddDays(30);
            var expiringDrivers = await _context.Drivers
                .Include(d => d.User)
                .Where(d => d.LicenseExpiryDate <= thirtyDaysAhead && d.LicenseExpiryDate >= now)
                .ToListAsync();

            foreach (var driver in expiringDrivers)
            {
                var title = $"License Expiry Notice: {driver.FullName}";
                bool alreadySent = await _context.Notifications.AnyAsync(n => n.Title == title && n.CreatedAt >= now.AddDays(-7));
                if (!alreadySent)
                {
                    if (!string.IsNullOrEmpty(driver.UserId))
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = driver.UserId,
                            Title = "License Renewal Reminder",
                            Message = $"Your driver license ({driver.LicenseNumber}) expires on {driver.LicenseExpiryDate:MMM dd, yyyy}. Please renew to maintain active fleet authorization.",
                            Type = NotificationType.LicenseExpiry,
                            ActionUrl = "/Account/Profile",
                            CreatedAt = now
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
