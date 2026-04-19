using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Areas.Customer;
using WebBanHang.Models;

namespace WebBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ProductController : CustomerAreaControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }

        [AllowAnonymous]
        public IActionResult Index(string? keyword, string? q, int? categoryId, int? authorId, int? genreId, int? publisherId, int page = 1)
        {
            var kw = !string.IsNullOrWhiteSpace(keyword) ? keyword : q;
            return RedirectToAction("Index", "Home", new
            {
                area = "Customer",
                tab = "products",
                keyword = kw,
                categoryId,
                authorId,
                genreId,
                publisherId,
                page
            });
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Author)
                .Include(p => p.Genre)
                .Include(p => p.Publisher)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.HasBookPreview = await _db.BookPreviews.AsNoTracking().AnyAsync(x => x.BookId == id);
            // Đặt chỗ chỉ khi không còn bản tại shop nhưng vẫn có phiếu mượn đang mở (sách đang ở khách).
            ViewBag.ShowReservePlace = product.Stock <= 0 &&
                await _db.Borrows.AsNoTracking().AnyAsync(b =>
                    b.BookId == id && b.Status == BorrowStatus.Borrowing);
            ViewData["Title"] = product.Name;
            ViewData["CatalogFilterHidden"] = null;
            ViewData["CustomerNavSection"] = "books";
            ViewData["CustomerPageTitle"] = product.Name;
            ViewData["CustomerBreadcrumb"] = "Tổng quan / Sách / Chi tiết";
            return View(product);
        }

        [HttpPost]
        [Authorize]
        public IActionResult AddReview(int productId, int rating, string? comment)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return NotFound();
            }

            var userId = User.Claims.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var review = new Review
            {
                ProductId = productId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            _db.SaveChanges();
            TempData["Success"] = "Đã thêm đánh giá";
            return RedirectToAction("Details", new { id = productId });
        }
    }
}
