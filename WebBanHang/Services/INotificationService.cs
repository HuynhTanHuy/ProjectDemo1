using WebBanHang.Models.DTOs;

namespace WebBanHang.Services
{
    public interface INotificationService
    {
        Task<AdminNotificationsDto> GetAdminNotificationsAsync(
            int maxItems = 12,
            DateTime? readAtUtc = null);
    }
}
