using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class PublishersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PublishersController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index(int page = 1)
        {
            const int pageSize = 12;
            var totalItems = _db.Publishers.Count();
            var publishers = _db.Publishers
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.TotalItems = totalItems;
            
            return View(publishers);
        }

        public IActionResult Details(int id)
        {
            var publisher = _db.Publishers
                .Include(p => p.Products)
                    .ThenInclude(pr => pr.Author)
                .Include(p => p.Products)
                    .ThenInclude(pr => pr.Genre)
                .FirstOrDefault(p => p.Id == id);

            if (publisher == null)
            {
                return NotFound();
            }

            return View(publisher);
        }
    }
}



