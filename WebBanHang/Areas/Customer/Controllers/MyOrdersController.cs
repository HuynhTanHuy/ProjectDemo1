using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Services;
using WebBanHang.Areas.Customer;

namespace WebBanHang.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class MyOrdersController : CustomerAreaControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IOrderCheckoutService _checkout;

        public MyOrdersController(ApplicationDbContext db, IOrderCheckoutService checkout)
        {
            _db = db;
            _checkout = checkout;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            ViewData["Title"] = "Đơn hàng của tôi";
            var uid = UserId;
            if (string.IsNullOrEmpty(uid))
            {
                return Challenge();
            }

            page = Math.Max(1, page);
            const int pageSize = 10;

            var query = _db.Orders
                .AsNoTracking()
                .Where(o => o.UserId == uid)
                .OrderByDescending(o => o.OrderDate);

            var total = await query.CountAsync();
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            ViewData["Title"] = "Chi tiết đơn hàng";
            var uid = UserId;
            if (string.IsNullOrEmpty(uid))
            {
                return Challenge();
            }

            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == uid);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Pay(int id)
        {
            var uid = UserId;
            if (string.IsNullOrEmpty(uid))
            {
                return Challenge();
            }

            var r = await _checkout.EnsurePendingPaymentAsync(uid, id);
            if (!r.Success || r.Data == 0)
            {
                TempData["Error"] = r.Message;
                return RedirectToAction(nameof(Details), new { id });
            }

            return RedirectToAction("Start", "Payment", new { id = r.Data });
        }

        [HttpGet]
        public IActionResult Completed(int id)
        {
            ViewData["Title"] = "Thanh toán thành công";
            return View(id);
        }

        [HttpGet]
        public IActionResult Failed(int id, string? reason = null)
        {
            ViewData["Title"] = "Thanh toán không thành công";
            ViewBag.Reason = reason;
            return View(id);
        }

        [HttpGet]
        public IActionResult Cancelled(int id)
        {
            ViewData["Title"] = "Đã hủy thanh toán";
            return View(id);
        }
    }
}
