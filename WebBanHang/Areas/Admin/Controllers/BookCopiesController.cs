using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class BookCopiesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBookCopyManagementService _management;
        private readonly IBookCopyQrService _copyQr;
        private readonly IWebHostEnvironment _host;

        public BookCopiesController(
            ApplicationDbContext db,
            IBookCopyManagementService management,
            IBookCopyQrService copyQr,
            IWebHostEnvironment host)
        {
            _db = db;
            _management = management;
            _copyQr = copyQr;
            _host = host;
        }

        public async Task<IActionResult> Index([FromQuery] BookCopyIndexViewModel? vm)
        {
            ViewData["Title"] = "Bản sao sách";
            ViewData["AdminNavSection"] = "book_copies";
            ViewData["AdminPageTitle"] = "Bản sao sách";
            ViewData["AdminBreadcrumb"] = "Tổng quan / Sách / Bản sao";
            ViewData["AdminNotifCount"] = await _db.Borrows.CountAsync(b =>
                b.Status == BorrowStatus.Overdue ||
                (b.Status == BorrowStatus.Borrowing && b.DueDate.Date < DateTime.UtcNow.Date));

            vm ??= new BookCopyIndexViewModel();
            var model = await _management.GetIndexAsync(vm);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateQr(int bookCopyId)
        {
            try
            {
                await _copyQr.RegenerateBookCopyQrAsync(bookCopyId);
                TempData["Success"] = "Đã tạo lại QR.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToIndexPreservingQuery();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadQr(int id)
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

        private IActionResult RedirectToIndexPreservingQuery()
        {
            var route = Request.Query.Keys.ToDictionary(
                k => k,
                k => (object)Request.Query[k].ToString());
            return RedirectToAction(nameof(Index), route);
        }
    }
}
