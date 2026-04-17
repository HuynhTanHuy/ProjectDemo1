using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using Microsoft.AspNetCore.Authorization;

namespace WebBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index(string? q, int? categoryId, int? authorId, int? genreId, int? publisherId, int page = 1)
        {
            // Redirect đến Home/Index với tab products
            return RedirectToAction("Index", "Home", new { 
                area = "Customer", 
                tab = "products", 
                q = q, 
                categoryId = categoryId, 
                authorId = authorId, 
                genreId = genreId, 
                publisherId = publisherId, 
                page = page 
            });
        }

        public IActionResult Details(int id)
        {
            var product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Author)
                .Include(p => p.Genre)
                .Include(p => p.Publisher)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [Authorize]
        public IActionResult AddReview(int productId, int rating, string? comment)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return NotFound();

            var userId = User.Claims.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

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