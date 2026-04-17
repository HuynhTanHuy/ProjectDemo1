using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBanHang.Extensions;
using WebBanHang.Models;
using WebBanHang.Repositories;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace WebBanHang.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const decimal SHIPPING_COST = 30000; // Phí vận chuyển cố định

        public ShoppingCartController(ApplicationDbContext context,
                                      UserManager<ApplicationUser> userManager,
                                      IProductRepository productRepository)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var product = await GetProductFromDatabase(productId);
            if (product == null)
            {
                TempData["Error"] = "Sản phẩm không tồn tại";
                return RedirectToAction("Index", "Home", new { area = "Customer" });
            }

            var cartItem = new CartItem
            {
                ProductId = productId,
                Name = product.Name,
                Price = product.DiscountPercent > 0
                        ? (decimal)(product.Price * (100 - product.DiscountPercent) / 100)
                        : (decimal)product.Price,
                Quantity = quantity
            };

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            cart.AddItem(cartItem);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng";
            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            ViewBag.ShippingCost = SHIPPING_COST;
            return View(cart);
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            if (quantity < 1) return BadRequest();

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    item.Quantity = quantity;
                    HttpContext.Session.SetObjectAsJson("Cart", cart);
                }
            }

            return Ok();
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart != null)
            {
                cart.RemoveItem(productId);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
                TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng";
            }

            return Ok();
        }

        [HttpPost]
        public IActionResult ApplyDiscount(string code)
        {
            // TODO: Implement discount logic
            // For now, just return success
            TempData["Success"] = "Đã áp dụng mã giảm giá";
            return Ok();
        }

        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng trống";
                return RedirectToAction("Index");
            }

            var order = new Order
            {
                TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity) + SHIPPING_COST
            };

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng trống";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để đặt hàng";
                return RedirectToAction("Index");
            }

            order.UserId = user.Id;
            order.OrderDate = DateTime.UtcNow;
            order.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity) + SHIPPING_COST;
            order.OrderDetails = cart.Items.Select(i => new OrderDetail
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList();

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");
            TempData["Success"] = "Đặt hàng thành công";

            return View("OrderCompleted", order.Id);
        }

        private async Task<Product> GetProductFromDatabase(int productId)
        {
            return await _productRepository.GetByIdAsync(productId);
        }
    }
}
