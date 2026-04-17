using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewBag.TotalProducts = _db.Products.Count();
            ViewBag.TotalCategories = _db.Categories.Count();
            ViewBag.TotalAuthors = _db.Authors.Count();
            ViewBag.TotalGenres = _db.Genres.Count();
            ViewBag.TotalPublishers = _db.Publishers.Count();
            ViewBag.TotalOrders = _db.Orders.Count();
            
            return View();
        }
    }
}

