using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBanHang.Models;

namespace WebBanHang.ViewComponents
{
    public class AdminUserMenuViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminUserMenuViewComponent(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Content(string.Empty);
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            return View(user);
        }
    }
}
