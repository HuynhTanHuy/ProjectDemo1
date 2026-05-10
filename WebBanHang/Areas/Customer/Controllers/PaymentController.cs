using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Services;
using WebBanHang.Services.PaymentGateway;
using WebBanHang.Areas.Customer;

namespace WebBanHang.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class PaymentController : CustomerAreaControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentSignatureService _signature;
        private readonly IPaymentProcessingService _paymentProcessing;

        public PaymentController(
            ApplicationDbContext db,
            IPaymentSignatureService signature,
            IPaymentProcessingService paymentProcessing)
        {
            _db = db;
            _signature = signature;
            _paymentProcessing = paymentProcessing;
        }

        [HttpGet]
        public async Task<IActionResult> Start(int id)
        {
            ViewData["Title"] = "Thanh toán đơn hàng";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var payment = await _db.Payments
                .AsNoTracking()
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null || payment.Order == null || payment.Order.UserId != userId)
            {
                return NotFound();
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                TempData["Info"] = "Giao dịch thanh toán không còn ở trạng thái chờ xử lý.";
                return RedirectToAction("Details", "MyOrders", new { id = payment.OrderId });
            }

            return View(payment);
        }

        [HttpGet]
        public async Task<IActionResult> SimulatedGateway(int id, string result = "success")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var payment = await _db.Payments
                .AsNoTracking()
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null || payment.Order == null || payment.Order.UserId != userId)
            {
                return NotFound();
            }

            result = result.Trim().ToLowerInvariant();
            if (result is not ("success" or "failed" or "cancelled" or "expired"))
            {
                result = "success";
            }

            var sig = _signature.Sign(payment.Id, payment.OrderId, payment.Amount, result);
            var amountStr = payment.Amount.ToString("F2", CultureInfo.InvariantCulture);
            var path = Url.Action("Return", "Payment", new { area = "Customer" }) ?? "";
            var callbackUrl = $"{path}?paymentId={payment.Id}&orderId={payment.OrderId}&amount={Uri.EscapeDataString(amountStr)}&result={Uri.EscapeDataString(result)}&sig={Uri.EscapeDataString(sig)}";

            ViewBag.CallbackUrl = callbackUrl;
            ViewBag.ResultLabel = result;
            ViewData["Title"] = "Cổng thanh toán (giả lập)";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Return(
            int paymentId,
            int orderId,
            string amount,
            string result,
            string sig)
        {
            if (!decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amountDecimal))
            {
                return BadRequest("Số tiền không hợp lệ.");
            }

            var raw = Request.QueryString.Value;
            var dto = new SimulatedPaymentReturnDto
            {
                PaymentId = paymentId,
                OrderId = orderId,
                Amount = amountDecimal,
                Result = result,
                Signature = sig,
                RawQueryForAudit = raw
            };

            var outcome = await _paymentProcessing.ProcessSimulatedReturnAsync(dto);

            return outcome.Code switch
            {
                "success" or "already_success" => RedirectToAction("Completed", "MyOrders", new { id = orderId }),
                "failed" => RedirectToAction("Failed", "MyOrders", new { id = orderId }),
                "cancelled" => RedirectToAction("Cancelled", "MyOrders", new { id = orderId }),
                "expired" => RedirectToAction("Failed", "MyOrders", new { id = orderId, reason = "expired" }),
                "stock_error" => RedirectToAction("Failed", "MyOrders", new { id = orderId, reason = "stock" }),
                _ => RedirectToAction("Failed", "MyOrders", new { id = orderId, reason = outcome.Code })
            };
        }
    }
}
