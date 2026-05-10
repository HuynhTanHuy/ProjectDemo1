using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class LibrarySettingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ISystemSettingsService _settingsService;

        public LibrarySettingsController(ApplicationDbContext db, ISystemSettingsService settingsService)
        {
            _db = db;
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Cấu hình mượn sách";
            ViewData["AdminNavSection"] = "library_settings";
            ViewData["AdminPageTitle"] = "Cấu hình mượn sách";
            ViewData["AdminBreadcrumb"] = "Tổng quan / Thư viện / Cấu hình";

            var row = await _settingsService.GetAsync();
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SystemSetting model)
        {
            ViewData["Title"] = "Cấu hình mượn sách";
            ViewData["AdminNavSection"] = "library_settings";
            ViewData["AdminPageTitle"] = "Cấu hình mượn sách";
            ViewData["AdminBreadcrumb"] = "Tổng quan / Thư viện / Cấu hình";

            if (model.DefaultBorrowDays > model.MaxBorrowDays)
            {
                ModelState.AddModelError(string.Empty, "Số ngày mượn mặc định không được lớn hơn số ngày tối đa.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var entity = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Id == SystemSetting.SingletonId);
            if (entity == null)
            {
                TempData["Error"] = "Chưa có bản ghi cấu hình trong CSDL.";
                return View(model);
            }

            entity.DefaultBorrowDays = model.DefaultBorrowDays;
            entity.MaxBorrowDays = model.MaxBorrowDays;
            entity.MaxBorrowBookPerUser = model.MaxBorrowBookPerUser;
            entity.BorrowFee = model.BorrowFee;
            entity.OverdueFeePerDay = model.OverdueFeePerDay;
            entity.RemindBeforeDueDays = model.RemindBeforeDueDays;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            _settingsService.InvalidateCache();
            TempData["Success"] = "Đã cập nhật cấu hình.";
            return RedirectToAction(nameof(Index));
        }
    }
}
