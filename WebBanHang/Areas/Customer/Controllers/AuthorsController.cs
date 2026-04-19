using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Areas.Customer;
using WebBanHang.Models;

namespace WebBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class AuthorsController : CustomerAreaControllerBase
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
                .AsNoTracking()
                .OrderBy(a => a.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalItems / pageSize));
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.Tab = (string?)null;
            ViewData["Title"] = "Tác giả";
            return View(authors);
        }

        public IActionResult Details(int id)
        {
            var author = _db.Authors
                .Include(a => a.Products)
                    .ThenInclude(p => p!.Category)
                .Include(a => a.Products)
                    .ThenInclude(p => p!.Genre)
                .FirstOrDefault(a => a.Id == id);

            if (author == null)
            {
                return NotFound();
            }

            ViewData["Title"] = author.Name;
            return View(author);
        }
    }
}
