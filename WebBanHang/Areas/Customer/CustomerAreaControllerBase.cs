using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebBanHang.Extensions;
using WebBanHang.Models;

namespace WebBanHang.Areas.Customer
{
    /// <summary>
    /// Thiết lập ViewBag dùng chung cho layout Customer (giỏ hàng, v.v.).
    /// </summary>
    public abstract class CustomerAreaControllerBase : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            ViewBag.CartItemCount = cart?.Items.Sum(i => i.Quantity) ?? 0;
            base.OnActionExecuting(context);
        }
    }
}
