using System.Collections.Generic;
using System.Threading.Tasks;
using VehicleFleetManagement.Models;

namespace VehicleFleetManagement.Services.Interfaces
{
    public interface INotificationService
    {
        Task<int> GetUnreadCountAsync(string userId);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 20);
        Task<bool> MarkAsReadAsync(int notificationId, string userId);
        Task<bool> MarkAllAsReadAsync(string userId);
        Task<Notification> CreateNotificationAsync(string userId, string title, string message, string type, string? actionUrl = null);
        Task NotifyAdminsAsync(string title, string message, string type, string? actionUrl = null);
        Task CheckAndGenerateFleetAlertsAsync();
    }
}
