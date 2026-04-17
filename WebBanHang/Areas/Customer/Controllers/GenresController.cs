using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class GenresController : Controller
    {
        private readonly ApplicationDbContext _db;

        public GenresController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index(int page = 1)
        {
            const int pageSize = 12;
            var totalItems = _db.Genres.Count();
            var genres = _db.Genres
                .OrderBy(g => g.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.TotalItems = totalItems;
            
            return View(genres);
        }

        public IActionResult Details(int id)
        {
            var genre = _db.Genres
                .Include(g => g.Products)
                    .ThenInclude(p => p.Author)
                .Include(g => g.Products)
                    .ThenInclude(p => p.Category)
                .FirstOrDefault(g => g.Id == id);

            if (genre == null)
            {
                return NotFound();
            }

            return View(genre);
        }
    }
}



