using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class PenaltiesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PenaltiesController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? q, bool? isPaid, int page = 1, int pageSize = 10)
        {
            page = Math.Max(1, page);

            var query = _db.Penalties
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Borrow)
                    .ThenInclude(x => x!.Book)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim().ToLower();
                query = query.Where(x =>
                    (x.User != null && x.User.UserName != null && x.User.UserName.ToLower().Contains(keyword)) ||
                    (x.Reason != null && x.Reason.ToLower().Contains(keyword)) ||
                    (x.Borrow != null && x.Borrow.Book != null && x.Borrow.Book.Name.ToLower().Contains(keyword)));
            }

            if (isPaid.HasValue)
            {
                query = query.Where(x => x.IsPaid == isPaid.Value);
            }

            var totalItems = await query.CountAsync();
            var penalties = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Query = q;
            ViewBag.IsPaid = isPaid;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.TotalAmount = await _db.Penalties.SumAsync(x => x.Amount);

            var model = penalties.Select(x => new PenaltyListItemViewModel
            {
                Id = x.Id,
                UserName = x.User?.UserName ?? "N/A",
                BookTitle = x.Borrow?.Book?.Name ?? "N/A",
                Amount = x.Amount,
                Reason = x.Reason,
                CreatedAt = x.CreatedAt,
                IsPaid = x.IsPaid,
                PaidAt = x.PaidAt
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var penalty = await _db.Penalties.FirstOrDefaultAsync(x => x.Id == id);
            if (penalty == null)
            {
                return NotFound();
            }

            if (!penalty.IsPaid)
            {
                penalty.IsPaid = true;
                penalty.PaidAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Penalty marked as paid.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
