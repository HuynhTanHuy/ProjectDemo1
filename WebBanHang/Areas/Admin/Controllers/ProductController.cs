using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebBanHang.Helpers;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment hostEnvironment)
        {
            _db = db;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index([FromQuery] int? genreId, [FromQuery] string? status)
        {
            ViewData["Title"] = "Danh sách sách";
            ViewData["AdminNavSection"] = "books";
            ViewData["AdminPageTitle"] = "Danh sách sách";
            ViewData["AdminBreadcrumb"] = "Tổng quan / Sách";
            ViewData["AdminNotifCount"] = await _db.Borrows.CountAsync(b =>
                b.Status == BorrowStatus.Borrowing && b.DueDate.Date < DateTime.UtcNow.Date);

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

        public IActionResult Upsert(int? id)
        {
            ViewBag.Categories = _db.Categories.Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            });

            if (id == null || id == 0)
            {
                // Create Product
                return View(new Product());
            }
            else
            {
                // Update Product
                var product = _db.Products.FirstOrDefault(u => u.Id == id);
                return View(product);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Product product, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    if (file != null)
                    {
                        string fileName = Guid.NewGuid().ToString();
                        var uploads = Path.Combine(wwwRootPath, @"images\products");
                        var extension = Path.GetExtension(file.FileName);

                        // Create directory if it doesn't exist
                        if (!Directory.Exists(uploads))
                        {
                            Directory.CreateDirectory(uploads);
                        }

                        if (product.ImageUrl != null)
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('\\'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                        {
                            file.CopyTo(fileStreams);
                        }
                        product.ImageUrl = @"\images\products\" + fileName + extension;
                    }

                    if (product.Id == 0)
                    {
                        _db.Products.Add(product);
                        TempData["success"] = "Product created successfully";
                    }
                    else
                    {
                        _db.Products.Update(product);
                        TempData["success"] = "Product updated successfully";
                    }
                    _db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving product: " + ex.Message);
                }
            }
            else
            {
                // Reload Categories if validation fails
                ViewBag.Categories = _db.Categories.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                });
            }
            return View(product);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var productList = _db.Products.Include(p => p.Category);
            return Json(new { data = productList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _db.Products.FirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _db.Products.Remove(obj);
            _db.SaveChanges();
            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
} 