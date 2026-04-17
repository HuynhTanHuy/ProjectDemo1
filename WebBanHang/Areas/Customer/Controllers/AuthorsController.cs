using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class AuthorsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AuthorsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index(int page = 1)
        {
            const int pageSize = 12;
            var totalItems = _db.Authors.Count();
            var authors = _db.Authors
                .OrderBy(a => a.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.TotalItems = totalItems;
            
            return View(authors);
        }

        public IActionResult Details(int id)
        {
            var author = _db.Authors
                .Include(a => a.Products)
                    .ThenInclude(p => p.Category)
                .Include(a => a.Products)
                    .ThenInclude(p => p.Genre)
                .FirstOrDefault(a => a.Id == id);

            if (author == null)
            {
                return NotFound();
            }

            return View(author);
        }
    }
}



