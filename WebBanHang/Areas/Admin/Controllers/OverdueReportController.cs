using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OverdueReportController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OverdueReportController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            ViewData["Title"] = "Báo cáo quá hạn";
            ViewData["AdminNavSection"] = "overdue_report";
            ViewData["AdminPageTitle"] = "Báo cáo quá hạn";
            ViewData["AdminBreadcrumb"] = "Tổng quan / Thư viện / Quá hạn";

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 100);
            var utc = DateTime.UtcNow;

            var query = _db.Borrows
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Book)
                .Where(x =>
                    x.Status == BorrowStatus.Overdue ||
                    (x.Status == BorrowStatus.Borrowing && x.DueDate.Date < utc.Date))
                .OrderByDescending(x => x.FineAmount)
                .ThenBy(x => x.DueDate);

            var total = await query.CountAsync();
            var totalFine = await query.SumAsync(x => (decimal?)x.FineAmount) ?? 0m;

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new BorrowListItemViewModel
                {
                    BorrowId = x.Id,
                    UserName = x.User != null ? x.User.UserName ?? x.User.Email ?? "—" : "—",
                    BookTitle = x.Book != null ? x.Book.Name : "—",
                    BorrowDate = x.BorrowDate,
                    DueDate = x.DueDate,
                    ReturnDate = x.ReturnDate,
                    Status = x.Status,
                    IsOverdue = true,
                    FineAmount = x.FineAmount,
                    OverdueDays = x.OverdueDays
                })
                .ToListAsync();

            var vm = new BorrowIndexViewModel
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = total
            };

            ViewBag.TotalFine = totalFine;
            return View(vm);
        }
    }
}
