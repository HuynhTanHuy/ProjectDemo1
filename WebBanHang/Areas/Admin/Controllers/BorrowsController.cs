using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class BorrowsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBorrowService _borrowService;
        private readonly IBorrowStatisticsService _borrowStats;

        public BorrowsController(
            ApplicationDbContext db,
            IBorrowService borrowService,
            IBorrowStatisticsService borrowStats)
        {
            _db = db;
            _borrowService = borrowService;
            _borrowStats = borrowStats;
        }

        public async Task<IActionResult> Index([FromQuery] BorrowIndexViewModel? vm)
        {
            ViewData["Title"] = "Mượn & trả";
            ViewData["AdminNavSection"] = "borrows";
            ViewData["AdminPageTitle"] = "Mượn & trả";
            ViewData["AdminBreadcrumb"] = "Tổng quan / Sách / Mượn trả";
            ViewData["AdminNotifCount"] = await _borrowStats.GetOverdueCountAsync();

            vm ??= new BorrowIndexViewModel();
            if (vm.PageNumber < 1) vm.PageNumber = 1;
            if (vm.PageSize < 1) vm.PageSize = 10;

            vm.CategoryOptions = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() })
                .ToListAsync();
            vm.CategoryOptions.Insert(0, new SelectListItem { Value = "", Text = "Tất cả danh mục" });

            vm.StatTotalBorrows = await _borrowStats.GetTotalBorrowCountAsync();
            vm.StatActiveBorrowing = await _borrowStats.GetCurrentBorrowingCountAsync();
            vm.StatOverdue = await _borrowStats.GetOverdueCountAsync();
            vm.StatReturned = await _borrowStats.GetReturnedCountAsync();

            var query = _db.Borrows
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Book)
                    .ThenInclude(b => b!.Category)
                .Include(x => x.BookCopy)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(vm.SearchQuery))
            {
                var s = vm.SearchQuery.Trim();
                query = query.Where(x =>
                    (x.User != null && x.User.UserName != null && x.User.UserName.Contains(s)) ||
                    (x.Book != null && x.Book.Name.Contains(s)));
            }

            if (vm.CategoryId is > 0)
            {
                query = query.Where(x => x.Book != null && x.Book.CategoryId == vm.CategoryId);
            }

            if (!string.IsNullOrWhiteSpace(vm.Status))
            {
                switch (vm.Status)
                {
                    case "Returned":
                        query = query.Where(x => x.Status == BorrowStatus.Returned);
                        break;
                    case "Borrowing":
                        query = query.Where(x =>
                            x.Status == BorrowStatus.Borrowing &&
                            x.DueDate.Date >= DateTime.UtcNow.Date);
                        break;
                    case "Overdue":
                        query = query.Where(x =>
                            x.Status == BorrowStatus.Overdue ||
                            (x.Status == BorrowStatus.Borrowing &&
                             x.DueDate.Date < DateTime.UtcNow.Date));
                        break;
                }
            }

            query = query.OrderByDescending(x => x.BorrowDate);

            vm.TotalCount = await query.CountAsync();
            var page = await query
                .Skip((vm.PageNumber - 1) * vm.PageSize)
                .Take(vm.PageSize)
                .ToListAsync();

            vm.Items = page.Select(x => new BorrowListItemViewModel
            {
                BorrowId = x.Id,
                BookCopyId = x.BookCopyId,
                CopyCode = x.BookCopy?.CopyCode,
                UserName = x.User?.UserName ?? "N/A",
                BookTitle = x.Book?.Name ?? "N/A",
                BorrowDate = x.BorrowDate,
                DueDate = x.DueDate,
                ReturnDate = x.ReturnDate,
                Status = x.Status,
                IsOverdue = x.Status == BorrowStatus.Overdue ||
                    (x.Status == BorrowStatus.Borrowing && x.DueDate.Date < DateTime.UtcNow.Date),
                FineAmount = x.FineAmount,
                OverdueDays = x.OverdueDays
            }).ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReturned(int borrowId)
        {
            var result = await _borrowService.AdminMarkReturnedAsync(borrowId);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
            }
            else
            {
                TempData["Success"] = "Đã ghi nhận trả sách.";
            }

            return RedirectToIndexPreservingQuery();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var borrow = await _db.Borrows.Include(x => x.Book).FirstOrDefaultAsync(x => x.Id == id);
            if (borrow == null)
            {
                TempData["Error"] = "Không tìm thấy phiếu mượn.";
                return RedirectToIndexPreservingQuery();
            }

            if (borrow.Status is BorrowStatus.Borrowing or BorrowStatus.Overdue)
            {
                TempData["Error"] = "Không thể xóa phiếu đang mượn hoặc quá hạn. Vui lòng ghi nhận trả sách trước.";
                return RedirectToIndexPreservingQuery();
            }

            _db.Borrows.Remove(borrow);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa phiếu mượn.";
            return RedirectToIndexPreservingQuery();
        }

        private IActionResult RedirectToIndexPreservingQuery()
        {
            var route = Request.Query.Keys.ToDictionary(
                k => k,
                k => (object)Request.Query[k].ToString());
            return RedirectToAction(nameof(Index), route);
        }
    }
}
