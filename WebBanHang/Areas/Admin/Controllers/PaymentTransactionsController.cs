using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class PaymentTransactionsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PaymentTransactionsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 30)
        {
            ViewData["Title"] = "Nhật ký thanh toán";
            ViewData["AdminNavSection"] = "payment_tx";
            ViewData["AdminPageTitle"] = "Nhật ký thanh toán";
            ViewData["AdminBreadcrumb"] = "Tổng quan / Giao dịch / Thanh toán";

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 200);

            var query = _db.PaymentTransactions
                .AsNoTracking()
                .Include(t => t.Payment)
                .ThenInclude(p => p!.Order)
                .OrderByDescending(t => t.Id);

            var total = await query.CountAsync();
            var rows = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalCount = total;
            return View(rows);
        }
    }
}
