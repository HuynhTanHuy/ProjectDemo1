using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            page = Math.Max(1, page);
            var query = _db.Borrows
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Book)
                .OrderByDescending(x => x.BorrowDate);

            var totalItems = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var result = items.Select(x => new BorrowListItemViewModel
            {
                BorrowId = x.Id,
                UserName = x.User?.UserName ?? "N/A",
                BookTitle = x.Book?.Name ?? "N/A",
                BorrowDate = x.BorrowDate,
                DueDate = x.DueDate,
                ReturnDate = x.ReturnDate,
                Status = x.Status,
                IsOverdue = x.Status == BorrowStatus.Borrowing && x.DueDate < DateTime.UtcNow
            }).ToList();

            return View(result);
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
                TempData["Error"] = "Borrow already returned.";
                return RedirectToAction(nameof(Index));
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
                    Reason = $"Late return ({lateDays} day(s)).",
                    CreatedAt = DateTime.UtcNow,
                    IsPaid = false
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Marked as returned successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
