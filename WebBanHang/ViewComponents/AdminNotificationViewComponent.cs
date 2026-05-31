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
            var model = await _notificationService.GetAdminNotificationsAsync();
            return View(model);
        }
    }
}
