using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class PublishersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostEnvironment;

        public PublishersController(ApplicationDbContext db, IWebHostEnvironment hostEnvironment)
        {
            _db = db;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            IEnumerable<Publisher> publishers = _db.Publishers.OrderBy(p => p.Name);
            return View(publishers);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Publisher obj, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    if (file != null)
                    {
                        string fileName = Guid.NewGuid().ToString();
                        var uploads = Path.Combine(wwwRootPath, @"images\publishers");
                        var extension = Path.GetExtension(file.FileName);

                        if (!Directory.Exists(uploads))
                        {
                            Directory.CreateDirectory(uploads);
                        }

                        using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                        {
                            file.CopyTo(fileStreams);
                        }
                        obj.LogoUrl = @"\images\publishers\" + fileName + extension;
                    }

                    _db.Publishers.Add(obj);
                    _db.SaveChanges();
                    TempData["success"] = "Nhà xuất bản đã được tạo thành công";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi lưu nhà xuất bản: " + ex.Message);
                }
            }
            return View(obj);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var publisher = _db.Publishers.Find(id);
            if (publisher == null)
            {
                return NotFound();
            }
            return View(publisher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Publisher obj, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    if (file != null)
                    {
                        string fileName = Guid.NewGuid().ToString();
                        var uploads = Path.Combine(wwwRootPath, @"images\publishers");
                        var extension = Path.GetExtension(file.FileName);

                        if (!Directory.Exists(uploads))
                        {
                            Directory.CreateDirectory(uploads);
                        }

                        if (obj.LogoUrl != null)
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, obj.LogoUrl.TrimStart('\\'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                        {
                            file.CopyTo(fileStreams);
                        }
                        obj.LogoUrl = @"\images\publishers\" + fileName + extension;
                    }

                    _db.Publishers.Update(obj);
                    _db.SaveChanges();
                    TempData["success"] = "Nhà xuất bản đã được cập nhật thành công";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật nhà xuất bản: " + ex.Message);
                }
            }
            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var publisher = _db.Publishers.Find(id);
            if (publisher == null)
            {
                return NotFound();
            }
            return View(publisher);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var publisher = _db.Publishers.Find(id);
            if (publisher == null)
            {
                return NotFound();
            }

            try
            {
                if (publisher.LogoUrl != null)
                {
                    var imagePath = Path.Combine(_hostEnvironment.WebRootPath, publisher.LogoUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _db.Publishers.Remove(publisher);
                _db.SaveChanges();
                TempData["success"] = "Nhà xuất bản đã được xóa thành công";
            }
            catch (Exception ex)
            {
                TempData["error"] = "Lỗi khi xóa nhà xuất bản: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}

