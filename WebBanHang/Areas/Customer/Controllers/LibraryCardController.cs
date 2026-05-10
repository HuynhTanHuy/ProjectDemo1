using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanHang.Areas.Customer;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services;

namespace WebBanHang.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class LibraryCardController : CustomerAreaControllerBase
    {
        private readonly ILibraryMemberQrService _memberQr;
        private readonly IWebHostEnvironment _host;

        public LibraryCardController(ILibraryMemberQrService memberQr, IWebHostEnvironment host)
        {
            _memberQr = memberQr;
            _host = host;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Thẻ thư viện (QR)";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var result = await _memberQr.EnsureMemberCardAsync(userId);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(new LibraryMemberCardViewModel());
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadQr()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var result = await _memberQr.EnsureMemberCardAsync(userId);
            if (!result.Success || result.Data?.QrImageUrl == null)
            {
                return NotFound();
            }

            var trimmed = result.Data.QrImageUrl.TrimStart('~', '/', '\\');
            var physical = Path.Combine(_host.WebRootPath, trimmed.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(physical))
            {
                return NotFound();
            }

            return PhysicalFile(physical, "image/png", "the-thu-vien.png");
        }
    }
}
