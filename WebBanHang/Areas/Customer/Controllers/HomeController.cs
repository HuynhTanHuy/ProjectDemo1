using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebBanHang.Areas.Customer;
using WebBanHang.Helpers;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;

namespace WebBanHang.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class HomeController : CustomerAreaControllerBase
    {
        private const int TabPageSize = 8;

        private readonly ApplicationDbContext _db;
        private readonly ILogger<HomeController> _logger;
        private readonly IHostEnvironment _env;

        public HomeController(ApplicationDbContext db, ILogger<HomeController> logger, IHostEnvironment env)
        {
            _db = db;
            _logger = logger;
            _env = env;
        }

        private static int NormalizePageSize(int? pageSize)
        {
            int[] allowed = { 12, 24, 48 };
            var v = pageSize ?? 12;
            return allowed.Contains(v) ? v : 12;
        }

        private static string NormalizeSort(string? sort)
        {
            var s = sort?.Trim().ToLowerInvariant() ?? "popular";
            return s is "newest" or "az" ? s : "popular";
        }

        private static List<KeyValuePair<string, string?>> BuildCatalogFilterHidden(CustomerHomeIndexViewModel model)
        {
            var list = new List<KeyValuePair<string, string?>> { new("tab", "products") };
            if (!string.IsNullOrWhiteSpace(model.Query))
            {
                list.Add(new KeyValuePair<string, string?>("keyword", model.Query));
            }
            if (model.CategoryId is > 0)
            {
                list.Add(new KeyValuePair<string, string?>("categoryId", model.CategoryId.ToString()));
            }

            if (model.AuthorId is > 0)
            {
                list.Add(new KeyValuePair<string, string?>("authorId", model.AuthorId.ToString()));
            }

            if (model.GenreId is > 0)
            {
                list.Add(new KeyValuePair<string, string?>("genreId", model.GenreId.ToString()));
            }

            if (model.PublisherId is > 0)
            {
                list.Add(new KeyValuePair<string, string?>("publisherId", model.PublisherId.ToString()));
            }

            if (!string.IsNullOrWhiteSpace(model.Status))
            {
                list.Add(new KeyValuePair<string, string?>("status", model.Status));
            }

            list.Add(new KeyValuePair<string, string?>("sort", model.Sort));
            if (model.MinRating is >= 1 and <= 5)
            {
                list.Add(new KeyValuePair<string, string?>("minRating", model.MinRating.ToString()));
            }

            if (model.YearFrom is > 0)
            {
                list.Add(new KeyValuePair<string, string?>("yearFrom", model.YearFrom.ToString()));
            }

            if (model.YearTo is > 0)
            {
                list.Add(new KeyValuePair<string, string?>("yearTo", model.YearTo.ToString()));
            }

            if (!string.IsNullOrWhiteSpace(model.Lang))
            {
                list.Add(new KeyValuePair<string, string?>("lang", model.Lang));
            }

            list.Add(new KeyValuePair<string, string?>("pageSize", model.PageSize.ToString()));
            return list;
        }

        /// <summary>Chuẩn catalog: chỉ query key "keyword" (model binding + fallback Request.Query).</summary>
        private static string? ResolveSearchKeyword(HttpRequest request, string? keyword)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                return keyword.Trim();
            }

            if (!request.Query.TryGetValue("keyword", out var values))
            {
                return null;
            }

            var s = values.FirstOrDefault();
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index(
            [FromQuery(Name = "keyword")] string? keyword,
            [FromQuery] int? categoryId,
            [FromQuery] int? authorId,
            [FromQuery] int? genreId,
            [FromQuery] int? publisherId,
            [FromQuery] string? tab = "products",
            [FromQuery] string? status = null,
            [FromQuery] string? sort = null,
            [FromQuery] int? minRating = null,
            [FromQuery] int? rating = null,
            [FromQuery] int? yearFrom = null,
            [FromQuery] int? yearTo = null,
            [FromQuery] int? year = null,
            [FromQuery] string? lang = null,
            [FromQuery] string? language = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] int page = 1)
        {
            page = Math.Max(1, page);
            tab = tab?.ToLowerInvariant() ?? "products";
            var pageSizeNorm = NormalizePageSize(pageSize);
            var sortNorm = NormalizeSort(sort);

            var searchText = ResolveSearchKeyword(Request, keyword);

            if (_env.IsDevelopment())
            {
                var rawKeyword = Request.Query["keyword"].ToString();
                _logger.LogInformation(
                    "Customer Home Index: QueryString={QueryString}; Request.Query[keyword]={Raw}; bound keyword param={Bound}; resolved={Resolved}; all keys: {Keys}",
                    Request.QueryString.Value,
                    string.IsNullOrEmpty(rawKeyword) ? "(empty)" : rawKeyword,
                    keyword ?? "(null)",
                    searchText ?? "(null)",
                    string.Join(", ", Request.Query.Keys));
            }

            var ratingFilter = rating is >= 1 and <= 5 ? rating : minRating;
            var langFilter = !string.IsNullOrWhiteSpace(language) ? language.Trim() : lang;

            int? yFrom = yearFrom is > 0 ? yearFrom : null;
            int? yTo = yearTo is > 0 ? yearTo : null;
            if (year is > 0 && yFrom == null && yTo == null)
            {
                yFrom = year;
                yTo = year;
            }

            var model = new CustomerHomeIndexViewModel
            {
                Tab = tab,
                Query = searchText,
                CategoryId = categoryId,
                AuthorId = authorId,
                GenreId = genreId,
                PublisherId = publisherId,
                Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant(),
                Sort = sortNorm,
                MinRating = ratingFilter is >= 1 and <= 5 ? ratingFilter : null,
                YearFrom = yFrom,
                YearTo = yTo,
                Lang = string.IsNullOrWhiteSpace(langFilter) ? null : langFilter.Trim(),
                PageSize = pageSizeNorm,
                CurrentPage = page
            };

            var overdueNotif = await _db.Borrows.CountAsync(b =>
                b.Status == BorrowStatus.Borrowing && b.DueDate.Date < DateTime.UtcNow.Date);

            ViewData["Title"] = tab switch
            {
                "authors" => "Tác giả",
                "genres" => "Thể loại",
                "publishers" => "Nhà xuất bản",
                _ => "Danh sách sách"
            };

            ViewData["CatalogFilterHidden"] = null;

            switch (tab)
            {
                case "authors":
                {
                    var authorsQ = _db.Authors.AsNoTracking();
                    if (!string.IsNullOrWhiteSpace(model.Query))
                    {
                        var t = model.Query.Trim().ToLowerInvariant();
                        authorsQ = authorsQ.Where(a => a.Name.ToLower().Contains(t));
                    }

                    var authorsTotal = await authorsQ.CountAsync();
                    model.Authors = await authorsQ
                        .OrderBy(a => a.Name)
                        .Skip((page - 1) * TabPageSize)
                        .Take(TabPageSize)
                        .ToListAsync();
                    model.TotalItems = authorsTotal;
                    model.TotalPages = Math.Max(1, (int)Math.Ceiling(authorsTotal / (double)TabPageSize));
                    break;
                }

                case "genres":
                {
                    var genresQ = _db.Genres.AsNoTracking();
                    if (!string.IsNullOrWhiteSpace(model.Query))
                    {
                        var t = model.Query.Trim().ToLowerInvariant();
                        genresQ = genresQ.Where(g => g.Name.ToLower().Contains(t));
                    }

                    var genresTotal = await genresQ.CountAsync();
                    model.Genres = await genresQ
                        .OrderBy(g => g.Name)
                        .Skip((page - 1) * TabPageSize)
                        .Take(TabPageSize)
                        .ToListAsync();
                    model.TotalItems = genresTotal;
                    model.TotalPages = Math.Max(1, (int)Math.Ceiling(genresTotal / (double)TabPageSize));
                    break;
                }

                case "publishers":
                {
                    var publishersQ = _db.Publishers.AsNoTracking();
                    if (!string.IsNullOrWhiteSpace(model.Query))
                    {
                        var t = model.Query.Trim().ToLowerInvariant();
                        publishersQ = publishersQ.Where(p => p.Name.ToLower().Contains(t));
                    }

                    var publishersTotal = await publishersQ.CountAsync();
                    model.Publishers = await publishersQ
                        .OrderBy(p => p.Name)
                        .Skip((page - 1) * TabPageSize)
                        .Take(TabPageSize)
                        .ToListAsync();
                    model.TotalItems = publishersTotal;
                    model.TotalPages = Math.Max(1, (int)Math.Ceiling(publishersTotal / (double)TabPageSize));
                    break;
                }

                default:
                    // Từ khóa tìm: URL thường là ?keyword=... → model.Query; record BookCatalogQuery gọi thuộc tính là Search.
                    model.Catalog = await BookCatalogHelper.BuildAsync(_db, new BookCatalogQuery(
                        GenreId: model.GenreId,
                        Status: model.Status,
                        CategoryId: model.CategoryId,
                        AuthorId: model.AuthorId,
                        PublisherId: model.PublisherId,
                        Search: model.Query,
                        Page: page,
                        PageSize: model.PageSize,
                        MinRating: model.MinRating,
                        YearFrom: model.YearFrom,
                        YearTo: model.YearTo,
                        Lang: model.Lang,
                        Sort: model.Sort));
                    model.TotalItems = model.Catalog.TotalFilteredCount;
                    model.TotalPages = Math.Max(1, (int)Math.Ceiling(model.TotalItems / (double)model.PageSize));
                    ViewBag.StatusFilter = model.Catalog.Status;
                    ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
                    ViewBag.Authors = await _db.Authors.OrderBy(a => a.Name).ToListAsync();
                    ViewBag.Genres = await _db.Genres.OrderBy(g => g.Name).ToListAsync();
                    ViewBag.Publishers = await _db.Publishers.OrderBy(p => p.Name).ToListAsync();
                    ViewBag.Languages = await _db.Products
                        .AsNoTracking()
                        .Where(p => p.Language != null && p.Language != "")
                        .Select(p => p.Language!)
                        .Distinct()
                        .OrderBy(l => l)
                        .ToListAsync();
                    ViewData["CatalogFilterHidden"] = BuildCatalogFilterHidden(model);
                    break;
            }

            ViewBag.Tab = model.Tab;
            ViewBag.Query = model.Query;
            ViewBag.SelectedCategoryId = model.CategoryId;
            ViewBag.SelectedAuthorId = model.AuthorId;
            ViewBag.SelectedGenreId = model.GenreId;
            ViewBag.SelectedPublisherId = model.PublisherId;
            ViewBag.CurrentPage = model.CurrentPage;
            ViewBag.TotalPages = model.TotalPages;
            ViewBag.TotalItems = model.TotalItems;
            ViewBag.PageSize = model.Tab == "products" ? model.PageSize : TabPageSize;
            ViewBag.Sort = model.Sort;
            ViewBag.MinRating = model.MinRating;
            ViewBag.YearFrom = model.YearFrom;
            ViewBag.YearTo = model.YearTo;
            ViewBag.Lang = model.Lang;
            ViewBag.CustomerNotifCount = overdueNotif;

            return View(model);
        }

        public IActionResult Details(int id)
        {
            var product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Author)
                .Include(p => p.Genre)
                .Include(p => p.Publisher)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewData["Title"] = product.Name;
            ViewData["CatalogFilterHidden"] = null;
            return View(product);
        }
    }
}
