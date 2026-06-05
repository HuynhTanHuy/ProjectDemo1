using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanHang.Areas.Customer;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services;

namespace WebBanHang.Controllers
{
    /// <summary>
    /// Mượn/trả sách — thuộc Area Customer (URL: /Customer/Borrow/...).
    /// </summary>
    [Area("Customer")]
    [Authorize]
    public class BorrowController : CustomerAreaControllerBase
    {
        private readonly IBorrowService _borrowService;
        private readonly ILibraryQrWorkflowService _qrWorkflow;

        public BorrowController(IBorrowService borrowService, ILibraryQrWorkflowService qrWorkflow)
        {
            _borrowService = borrowService;
            _qrWorkflow = qrWorkflow;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BorrowBook(int bookId)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var result = await _borrowService.BorrowBookAsync(userId, bookId);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction("Details", "Product", new { area = "Customer", id = bookId });
            }

            TempData["Success"] = "Đã tạo phiếu mượn thành công.";
            return RedirectToAction(nameof(MyBorrows));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LookupBookCopyByQr([FromForm] string copyPayload)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new { success = false, code = "auth", message = "Chưa đăng nhập." });
            }

            var result = await _qrWorkflow.LookupCopyAsync(copyPayload);
            if (!result.Success || result.Data == null)
            {
                return Json(new { success = false, code = result.ErrorCode, message = result.Message });
            }

            var d = result.Data;
            var isBorrowedByMe = d.BorrowedByUserId == userId;
            var canReturn = isBorrowedByMe &&
                            d.ActiveBorrowStatus is BorrowStatus.Borrowing or BorrowStatus.Overdue;

            return Json(new
            {
                success = true,
                data = new
                {
                    d.BookCopyId,
                    d.BookId,
                    d.BookTitle,
                    d.AuthorName,
                    d.CategoryName,
                    d.BookImageUrl,
                    d.CopyCode,
                    d.ShelfLocation,
                    d.CopyStatus,
                    physicalStatus = (int)d.PhysicalStatus,
                    isBorrowedByMe,
                    canReturn,
                    dueDateUtc = isBorrowedByMe ? d.DueDateUtc : null,
                    productUrl = Url.Action("Details", "Product", new { area = "Customer", id = d.BookId })
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBookByQr([FromForm] string copyPayload)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Json(new { success = false, code = "auth", message = "Chưa đăng nhập." });
            }

            var result = await _borrowService.ReturnBookWithCopyPayloadAsync(userId, copyPayload);
            return Json(new { success = result.Success, code = result.ErrorCode, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBook(int borrowId)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var result = await _borrowService.ReturnBookAsync(userId, borrowId);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
            }
            else
            {
                TempData["Success"] = "Đã ghi nhận trả sách.";
            }

            return RedirectToAction(nameof(MyBorrows));
        }

        [HttpGet]
        public async Task<IActionResult> MyBorrows()
        {
            ViewData["Title"] = "Sách đang mượn";
            ViewData["CustomerNavSection"] = "borrow";
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var items = await _borrowService.GetActiveBorrowsForUserAsync(userId);
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> BorrowHistory(int page = 1)
        {
            ViewData["Title"] = "Lịch sử mượn sách";
            ViewData["CustomerNavSection"] = "borrow";
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var pageResult = await _borrowService.GetBorrowHistoryForUserAsync(userId, page, 10);
            var vm = new BorrowHistoryPageViewModel { Page = pageResult };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            ViewData["Title"] = "Chi tiết phiếu mượn";
            ViewData["CustomerNavSection"] = "borrow";
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var vm = await _borrowService.GetBorrowDetailForUserAsync(userId, id);
            if (vm == null)
            {
                return NotFound();
            }

            return View(vm);
        }
    }
}
