using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Helpers;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IProductBookCopyProvisioningService _bookCopyProvisioning;

        public ProductController(
            ApplicationDbContext db,
            IWebHostEnvironment hostEnvironment,
            IProductBookCopyProvisioningService bookCopyProvisioning)
        {
            _db = db;
            _hostEnvironment = hostEnvironment;
            _bookCopyProvisioning = bookCopyProvisioning;
        }

        public async Task<IActionResult> Index([FromQuery] int? genreId, [FromQuery] string? status)
        {
            ViewData["Title"] = "Danh sách sách";
            ViewData["AdminNavSection"] = "books";
            ViewData["AdminPageTitle"] = "Danh sách sách";
            ViewData["AdminBreadcrumb"] = "Tổng quan / Sách";

            var vm = await BookCatalogHelper.BuildAsync(_db, new BookCatalogQuery(
                genreId,
                status,
                null,
                null,
                null,
                null,
                1,
                50_000));

            return View(vm);
        }

        public async Task<IActionResult> Upsert(int? id)
        {
            SetAdminPageMeta(id.HasValue && id > 0 ? "Sửa sách" : "Thêm sách");

            Product product;
            if (id == null || id == 0)
            {
                product = new Product { Stock = 1 };
            }
            else
            {
                product = await _db.Products.FirstOrDefaultAsync(u => u.Id == id);
                if (product == null)
                {
                    return NotFound();
                }
            }

            PopulateBookSelectLists(product);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Product product, IFormFile? file)
        {
            SetAdminPageMeta(product.Id == 0 ? "Thêm sách" : "Sửa sách");

            if (ModelState.IsValid)
            {
                try
                {
                    var wwwRootPath = _hostEnvironment.WebRootPath;
                    if (file != null)
                    {
                        var fileName = Guid.NewGuid().ToString();
                        var uploads = Path.Combine(wwwRootPath, @"images\products");
                        var extension = Path.GetExtension(file.FileName);

                        if (!Directory.Exists(uploads))
                        {
                            Directory.CreateDirectory(uploads);
                        }

                        if (!string.IsNullOrEmpty(product.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('\\', '/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        await using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                        {
                            await file.CopyToAsync(fileStreams);
                        }

                        product.ImageUrl = @"\images\products\" + fileName + extension;
                    }

                    if (product.Id == 0)
                    {
                        _db.Products.Add(product);
                        TempData["Success"] = "Đã thêm sách mới.";
                    }
                    else
                    {
                        _db.Products.Update(product);
                        TempData["Success"] = "Đã cập nhật sách.";
                    }

                    await _db.SaveChangesAsync();
                    await _bookCopyProvisioning.SyncProductCopiesAsync(product.Id);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi lưu sách: " + ex.Message);
                }
            }

            PopulateBookSelectLists(product);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products
                .Include(p => p.BookCopies)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sách.";
                return RedirectToAction(nameof(Index));
            }

            var activeBorrows = await _db.Borrows.AnyAsync(b =>
                b.BookId == id &&
                (b.Status == BorrowStatus.Borrowing || b.Status == BorrowStatus.Overdue));

            if (activeBorrows)
            {
                TempData["Error"] = "Không thể xóa sách đang có phiếu mượn chưa trả.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var oldImagePath = Path.Combine(
                    _hostEnvironment.WebRootPath,
                    product.ImageUrl.TrimStart('\\', '/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã xóa sách.";
            return RedirectToAction(nameof(Index));
        }

        private void SetAdminPageMeta(string pageTitle)
        {
            ViewData["Title"] = pageTitle;
            ViewData["AdminNavSection"] = "books";
            ViewData["AdminPageTitle"] = pageTitle;
            ViewData["AdminBreadcrumb"] = "Tổng quan / Sách / " + pageTitle;
        }

        private void PopulateBookSelectLists(Product product)
        {
            ViewBag.Categories = _db.Categories
                .OrderBy(x => x.Name)
                .Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString(),
                    Selected = i.Id == product.CategoryId
                })
                .ToList();

            ViewBag.Authors = _db.Authors
                .OrderBy(x => x.Name)
                .Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString(),
                    Selected = product.AuthorId == i.Id
                })
                .ToList();
            ViewBag.Authors.Insert(0, new SelectListItem { Value = "", Text = "— Chọn tác giả —" });

            ViewBag.Genres = _db.Genres
                .OrderBy(x => x.Name)
                .Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString(),
                    Selected = product.GenreId == i.Id
                })
                .ToList();
            ViewBag.Genres.Insert(0, new SelectListItem { Value = "", Text = "— Chọn thể loại —" });

            ViewBag.Publishers = _db.Publishers
                .OrderBy(x => x.Name)
                .Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString(),
                    Selected = product.PublisherId == i.Id
                })
                .ToList();
            ViewBag.Publishers.Insert(0, new SelectListItem { Value = "", Text = "— Chọn nhà xuất bản —" });
        }
    }
}
