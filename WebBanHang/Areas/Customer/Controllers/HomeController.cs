using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebBanHang.Models;
using WebBanHang.Repositories;

namespace WebBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _db;

        public HomeController(IProductRepository productRepository, ApplicationDbContext db)
        {
            _productRepository = productRepository;
            _db = db;
        }

        public async Task<IActionResult> Index(string? q, int? categoryId, int? authorId, int? genreId, int? publisherId, string? tab = "products", int page = 1)
        {
            const int pageSize = 5;
            ViewBag.Tab = tab;
            ViewBag.Query = q;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedAuthorId = authorId;
            ViewBag.SelectedGenreId = genreId;
            ViewBag.SelectedPublisherId = publisherId;

            ViewBag.Categories = _db.Categories.OrderBy(c => c.Name).ToList();
            ViewBag.Authors = _db.Authors.OrderBy(a => a.Name).ToList();
            ViewBag.Genres = _db.Genres.OrderBy(g => g.Name).ToList();
            ViewBag.Publishers = _db.Publishers.OrderBy(p => p.Name).ToList();

            switch (tab?.ToLower() ?? "products")
            {
                case "authors":
                    var authorsTotal = _db.Authors.Count();
                    var authors = _db.Authors
                        .OrderBy(a => a.Name)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                    ViewBag.AuthorsList = authors;
                    ViewBag.CurrentPage = page;
                    ViewBag.TotalPages = (int)Math.Ceiling((double)authorsTotal / pageSize);
                    ViewBag.TotalItems = authorsTotal;
                    break;

                case "genres":
                    var genresTotal = _db.Genres.Count();
                    var genres = _db.Genres
                        .OrderBy(g => g.Name)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                    ViewBag.GenresList = genres;
                    ViewBag.CurrentPage = page;
                    ViewBag.TotalPages = (int)Math.Ceiling((double)genresTotal / pageSize);
                    ViewBag.TotalItems = genresTotal;
                    break;

                case "publishers":
                    var publishersTotal = _db.Publishers.Count();
                    var publishers = _db.Publishers
                        .OrderBy(p => p.Name)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                    ViewBag.PublishersList = publishers;
                    ViewBag.CurrentPage = page;
                    ViewBag.TotalPages = (int)Math.Ceiling((double)publishersTotal / pageSize);
                    ViewBag.TotalItems = publishersTotal;
                    break;

                default:
                    var query = _db.Products
                        .Include(p => p.Category)
                        .Include(p => p.Author)
                        .Include(p => p.Genre)
                        .Include(p => p.Publisher)
                        .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(q))
                    {
                        var lower = q.ToLower();
                        query = query.Where(p => p.Name.ToLower().Contains(lower) || 
                            (p.Description != null && p.Description.ToLower().Contains(lower)) || 
                            (p.Isbn != null && p.Isbn.Contains(q)));
                    }
                    if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
                    if (authorId.HasValue) query = query.Where(p => p.AuthorId == authorId);
                    if (genreId.HasValue) query = query.Where(p => p.GenreId == genreId);
                    if (publisherId.HasValue) query = query.Where(p => p.PublisherId == publisherId);

                    var productsTotal = query.Count();
                    var products = await query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                    ViewBag.Products = products;
                    ViewBag.CurrentPage = page;
                    ViewBag.TotalPages = (int)Math.Ceiling((double)productsTotal / pageSize);
                    ViewBag.TotalItems = productsTotal;
                    break;
            }

            return View();
        }

        public IActionResult Details(int id)
        {
            Product product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Author)
                .Include(p => p.Genre)
                .Include(p => p.Publisher)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
    }
}
