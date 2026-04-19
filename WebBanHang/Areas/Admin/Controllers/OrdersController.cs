using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OrdersController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Đơn hàng";
            ViewData["AdminNavSection"] = "orders";
            ViewData["AdminPageTitle"] = "Đơn hàng";
            ViewData["AdminBreadcrumb"] = "Tổng quan / Giao dịch / Đơn hàng";

            var orders = _db.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return View(orders);
        }

        public IActionResult Details(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var order = _db.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var order = _db.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Order obj)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = _db.Orders.Find(obj.Id);
                    if (existingOrder == null)
                    {
                        return NotFound();
                    }

                    existingOrder.OrderDate = obj.OrderDate;
                    existingOrder.TotalPrice = obj.TotalPrice;
                    existingOrder.ShippingAddress = obj.ShippingAddress;
                    existingOrder.Notes = obj.Notes;

                    _db.Orders.Update(existingOrder);
                    _db.SaveChanges();
                    TempData["success"] = "Đơn hàng đã được cập nhật thành công";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi cập nhật đơn hàng: " + ex.Message);
                }
            }
            
            obj.ApplicationUser = _db.Users.Find(obj.UserId);
            obj.OrderDetails = _db.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderId == obj.Id)
                .ToList();
            
            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var order = _db.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var order = _db.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            try
            {
                _db.OrderDetails.RemoveRange(order.OrderDetails);
                _db.Orders.Remove(order);
                _db.SaveChanges();
                TempData["success"] = "Đơn hàng đã được xóa thành công";
            }
            catch (Exception ex)
            {
                TempData["error"] = "Lỗi khi xóa đơn hàng: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}

