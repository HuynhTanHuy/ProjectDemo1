using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Areas.Customer;
using WebBanHang.Models;

namespace WebBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class PublishersController : CustomerAreaControllerBase
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
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalItems / pageSize));
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.Tab = (string?)null;
            ViewData["Title"] = "Nhà xuất bản";
            return View(publishers);
        }

        public IActionResult Details(int id)
        {
            var publisher = _db.Publishers
                .Include(p => p.Products)
                    .ThenInclude(pr => pr!.Author)
                .Include(p => p.Products)
                    .ThenInclude(pr => pr!.Genre)
                .FirstOrDefault(p => p.Id == id);

            if (publisher == null)
            {
                return NotFound();
            }

            ViewData["Title"] = publisher.Name;
            return View(publisher);
        }
    }
}
