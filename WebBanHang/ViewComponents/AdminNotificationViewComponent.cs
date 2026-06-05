using Microsoft.AspNetCore.Mvc;
using WebBanHang.Services;

namespace WebBanHang.ViewComponents
{
    public class AdminNotificationViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public AdminNotificationViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            DateTime? readAt = null;
            var raw = HttpContext.Session.GetString("AdminNotifReadAtUtc");
            if (!string.IsNullOrEmpty(raw) &&
                DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            {
                readAt = parsed;
            }

            var model = await _notificationService.GetAdminNotificationsAsync(readAtUtc: readAt);
            return View(model);
        }
    }
}
