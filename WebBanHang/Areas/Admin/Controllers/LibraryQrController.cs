using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Services;
using WebBanHang.Services.Results;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class LibraryQrController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILibraryQrWorkflowService _workflow;
        private readonly IBookInventoryService _inventory;
        private readonly IProductBookCopyProvisioningService _provisioning;
        private readonly IBookCopyQrService _copyQr;
        private readonly IWebHostEnvironment _host;

        public LibraryQrController(
            ApplicationDbContext db,
            ILibraryQrWorkflowService workflow,
            IBookInventoryService inventory,
            IProductBookCopyProvisioningService provisioning,
            IBookCopyQrService copyQr,
            IWebHostEnvironment host)
        {
            _db = db;
            _workflow = workflow;
            _inventory = inventory;
            _provisioning = provisioning;
            _copyQr = copyQr;
            _host = host;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "QR thư viện";
            ViewData["AdminNavSection"] = "library_qr";
            ViewData["AdminPageTitle"] = "QR & kiểm kê";
            ViewData["AdminBreadcrumb"] = "Tổng quan / QR thư viện";
            ViewData["AdminNotifCount"] = await _db.Borrows.CountAsync(b =>
                b.Status == BorrowStatus.Overdue ||
                (b.Status == BorrowStatus.Borrowing && b.DueDate.Date < DateTime.UtcNow.Date));

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LookupCopy([FromForm] string payload)
        {
            var result = await _workflow.LookupCopyAsync(payload);
            return Json(ToClient(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BorrowByQr([FromForm] string memberPayload, [FromForm] string copyPayload)
        {
            var result = await _workflow.BorrowWithMemberAndCopyAsync(memberPayload, copyPayload);
            return Json(ToClient(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnByQr([FromForm] string copyPayload)
        {
            var result = await _workflow.ReturnWithCopyAsync(copyPayload, asAdmin: true, memberUserIdWhenNotAdmin: null);
            return Json(ToClient(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartInventory([FromForm] string? note)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId))
            {
                return Json(ToClient(ServiceResult<int>.Fail("auth", "Không xác định được tài khoản quản trị.")));
            }

            var result = await _inventory.StartSessionAsync(adminId, note);
            return Json(ToClient(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScanInventory(
            [FromForm] int sessionId,
            [FromForm] string copyPayload,
            [FromForm] string? observedShelf)
        {
            var result = await _inventory.RecordScanAsync(sessionId, copyPayload, observedShelf);
            return Json(ToClient(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteInventory([FromForm] int sessionId)
        {
            var result = await _inventory.CompleteSessionAsync(sessionId);
            return Json(ToClient(result));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncAllCopies()
        {
            var n = await _provisioning.SyncAllProductsAsync();
            return Json(new { success = true, message = $"Đã đồng bộ bản sao cho {n} đầu sách." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateCopyQr([FromForm] int bookCopyId)
        {
            try
            {
                await _copyQr.RegenerateBookCopyQrAsync(bookCopyId);
                var copy = await _db.BookCopies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bookCopyId);
                return Json(new
                {
                    success = true,
                    message = "Đã tạo lại QR.",
                    data = new { copy?.QrImageRelativePath, copy?.CopyCode, copy?.QrPayload }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCopyQr(int id)
        {
            var copy = await _db.BookCopies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (copy?.QrImageRelativePath == null)
            {
                return NotFound();
            }

            var trimmed = copy.QrImageRelativePath.TrimStart('~', '/', '\\');
            var physical = Path.Combine(_host.WebRootPath, trimmed.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(physical))
            {
                return NotFound();
            }

            return PhysicalFile(physical, "image/png", $"{copy.CopyCode}.png");
        }

        private static object ToClient(ServiceResult r) =>
            new { success = r.Success, code = r.ErrorCode, message = r.Message };

        private static object ToClient<T>(ServiceResult<T> r) =>
            new { success = r.Success, code = r.ErrorCode, message = r.Message, data = r.Data };
    }
}
