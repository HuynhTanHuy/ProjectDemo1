using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class GenresController : Controller
    {
        private readonly ApplicationDbContext _db;

        public GenresController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            IEnumerable<Genre> genres = _db.Genres.OrderBy(g => g.Name);
            return View(genres);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Genre obj)
        {
            if (ModelState.IsValid)
            {
                _db.Genres.Add(obj);
                _db.SaveChanges();
                TempData["success"] = "Thể loại đã được tạo thành công";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var genre = _db.Genres.Find(id);
            if (genre == null)
            {
                return NotFound();
            }
            return View(genre);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Genre obj)
        {
            if (ModelState.IsValid)
            {
                _db.Genres.Update(obj);
                _db.SaveChanges();
                TempData["success"] = "Thể loại đã được cập nhật thành công";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var genre = _db.Genres.Find(id);
            if (genre == null)
            {
                return NotFound();
            }
            return View(genre);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var genre = _db.Genres.Find(id);
            if (genre == null)
            {
                return NotFound();
            }

            try
            {
                _db.Genres.Remove(genre);
                _db.SaveChanges();
                TempData["success"] = "Thể loại đã được xóa thành công";
            }
            catch (Exception ex)
            {
                TempData["error"] = "Lỗi khi xóa thể loại: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}

