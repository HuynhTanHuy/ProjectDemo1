using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class BorrowsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const decimal LateFeePerDay = 5000m;

        public BorrowsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index([FromQuery] BorrowIndexViewModel? vm)
        {
            ViewData["Title"] = "Mượn & trả";
            ViewData["AdminNavSection"] = "borrows";
            ViewData["AdminPageTitle"] = "Mượn & trả";
            ViewData["AdminBreadcrumb"] = "Tổng quan / Sách / Mượn trả";
            ViewData["AdminNotifCount"] = await _db.Borrows.CountAsync(b =>
                b.Status == BorrowStatus.Borrowing && b.DueDate.Date < DateTime.UtcNow.Date);

            vm ??= new BorrowIndexViewModel();
            if (vm.PageNumber < 1) vm.PageNumber = 1;
            if (vm.PageSize < 1) vm.PageSize = 10;

            vm.CategoryOptions = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() })
                .ToListAsync();
            vm.CategoryOptions.Insert(0, new SelectListItem { Value = "", Text = "Tất cả danh mục" });

            vm.StatTotalBorrows = await _db.Borrows.CountAsync();
            vm.StatActiveBorrowing = await _db.Borrows.CountAsync(x =>
                x.Status == BorrowStatus.Borrowing && x.DueDate.Date >= DateTime.UtcNow.Date);
            vm.StatOverdue = await _db.Borrows.CountAsync(x =>
                x.Status == BorrowStatus.Borrowing && x.DueDate.Date < DateTime.UtcNow.Date);
            vm.StatReturned = await _db.Borrows.CountAsync(x => x.Status == BorrowStatus.Returned);

            var query = _db.Borrows
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Book)
                    .ThenInclude(b => b!.Category)
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
                            x.Status == BorrowStatus.Borrowing &&
                            x.DueDate.Date < DateTime.UtcNow.Date);
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
                UserName = x.User?.UserName ?? "N/A",
                BookTitle = x.Book?.Name ?? "N/A",
                BorrowDate = x.BorrowDate,
                DueDate = x.DueDate,
                ReturnDate = x.ReturnDate,
                Status = x.Status,
                IsOverdue = x.Status == BorrowStatus.Borrowing && x.DueDate.Date < DateTime.UtcNow.Date
            }).ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReturned(int borrowId)
        {
            var borrow = await _db.Borrows.Include(x => x.Book).FirstOrDefaultAsync(x => x.Id == borrowId);
            if (borrow == null)
            {
                return NotFound();
            }

            if (borrow.Status == BorrowStatus.Returned)
            {
                TempData["Error"] = "Phiếu mượn đã được trả trước đó.";
                return RedirectToIndexPreservingQuery();
            }

            borrow.ReturnDate = DateTime.UtcNow;
            borrow.Status = BorrowStatus.Returned;
            if (borrow.Book != null)
            {
                borrow.Book.Stock += 1;
            }

            if (borrow.ReturnDate.Value > borrow.DueDate)
            {
                var lateDays = (borrow.ReturnDate.Value.Date - borrow.DueDate.Date).Days;
                _db.Penalties.Add(new Penalty
                {
                    UserId = borrow.UserId,
                    BorrowId = borrow.Id,
                    Amount = lateDays * LateFeePerDay,
                    Reason = $"Trả trễ ({lateDays} ngày).",
                    CreatedAt = DateTime.UtcNow,
                    IsPaid = false
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã ghi nhận trả sách.";
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

            if (borrow.Status == BorrowStatus.Borrowing)
            {
                TempData["Error"] = "Không thể xóa phiếu đang mượn. Vui lòng ghi nhận trả sách trước.";
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
