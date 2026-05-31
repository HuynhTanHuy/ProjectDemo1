using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UsersController : Controller
    {
        private readonly IUserManagementService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(IUserManagementService userService, UserManager<ApplicationUser> userManager)
        {
            _userService = userService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index([FromQuery] UserIndexViewModel? vm)
        {
            SetViewData("Danh sách tài khoản", "members", "Thành viên / Tài khoản");
            vm ??= new UserIndexViewModel();
            var model = await _userService.GetUsersAsync(vm);
            return View(model);
        }

        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var model = await _userService.GetUserDetailAsync(id);
            if (model == null) return NotFound();

            SetViewData("Chi tiết tài khoản", "members", "Thành viên / Chi tiết");
            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            SetViewData("Thêm tài khoản", "members", "Thành viên / Thêm");
            return View(new UserCreateViewModel
            {
                RoleOptions = await _userService.GetRoleSelectListAsync(SD.Role_Customer)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            model.RoleOptions = await _userService.GetRoleSelectListAsync(model.Role);
            if (!ModelState.IsValid) return View(model);

            var (success, error) = await _userService.CreateUserAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Không thể tạo tài khoản.");
                return View(model);
            }

            TempData["Success"] = "Đã tạo tài khoản thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserEditViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                FullName = user.FullName,
                Address = user.Address,
                Age = user.Age,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? SD.Role_Customer,
                RoleOptions = await _userService.GetRoleSelectListAsync(roles.FirstOrDefault())
            };

            SetViewData("Cập nhật tài khoản", "members", "Thành viên / Sửa");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            model.RoleOptions = await _userService.GetRoleSelectListAsync(model.Role);
            if (!ModelState.IsValid) return View(model);

            var (success, error) = await _userService.UpdateUserAsync(model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Không thể cập nhật tài khoản.");
                return View(model);
            }

            TempData["Success"] = "Đã cập nhật tài khoản.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        public async Task<IActionResult> ChangePassword(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            SetViewData("Đổi mật khẩu", "members", "Thành viên / Đổi mật khẩu");
            return View(new UserChangePasswordViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(UserChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetViewData("Đổi mật khẩu", "members", "Thành viên / Đổi mật khẩu");
                return View(model);
            }

            var (success, error) = await _userService.ChangePasswordAsync(model.Id, model.NewPassword);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Không thể đổi mật khẩu.");
                SetViewData("Đổi mật khẩu", "members", "Thành viên / Đổi mật khẩu");
                return View(model);
            }

            TempData["Success"] = "Đã đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(string id)
        {
            var currentUserId = _userManager.GetUserId(User) ?? "";
            var (success, error) = await _userService.LockUserAsync(id, currentUserId);
            TempData[success ? "Success" : "Error"] = success ? "Đã khóa tài khoản." : error;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var (success, error) = await _userService.UnlockUserAsync(id);
            TempData[success ? "Success" : "Error"] = success ? "Đã mở khóa tài khoản." : error;
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var model = await _userService.GetUserDetailAsync(id);
            if (model == null) return NotFound();

            SetViewData("Xóa tài khoản", "members", "Thành viên / Xóa");
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var currentUserId = _userManager.GetUserId(User) ?? "";
            var (success, error) = await _userService.DeleteUserAsync(id, currentUserId);
            if (!success)
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(Delete), new { id });
            }

            TempData["Success"] = "Đã xóa tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        private void SetViewData(string pageTitle, string nav, string breadcrumb)
        {
            ViewData["Title"] = pageTitle;
            ViewData["AdminNavSection"] = nav;
            ViewData["AdminPageTitle"] = pageTitle;
            ViewData["AdminBreadcrumb"] = breadcrumb;
        }
    }
}
