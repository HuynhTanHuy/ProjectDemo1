using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class NotificationsController : Controller
    {
        public const string ReadAtSessionKey = "AdminNotifReadAtUtc";

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkRead()
        {
            HttpContext.Session.SetString(ReadAtSessionKey, DateTime.UtcNow.ToString("O"));
            return Ok();
        }
    }
}
