using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services.Results;

namespace WebBanHang.Services
{
    public class LibraryMemberQrService : ILibraryMemberQrService
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LibraryMemberQrService> _logger;

        public LibraryMemberQrService(
            UserManager<ApplicationUser> users,
            IWebHostEnvironment env,
            ILogger<LibraryMemberQrService> logger)
        {
            _users = users;
            _env = env;
            _logger = logger;
        }

        public async Task<ServiceResult<LibraryMemberCardViewModel>> EnsureMemberCardAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _users.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user == null)
            {
                return ServiceResult<LibraryMemberCardViewModel>.Fail("user_not_found", "Không tìm thấy thành viên.");
            }

            if (string.IsNullOrEmpty(user.LibraryMemberQrToken))
            {
                user.LibraryMemberQrToken = Guid.NewGuid().ToString("N");
                var identityResult = await _users.UpdateAsync(user);
                if (!identityResult.Succeeded)
                {
                    return ServiceResult<LibraryMemberCardViewModel>.Fail(
                        "update_failed",
                        "Không thể tạo mã thẻ thư viện.");
                }
            }

            if (string.IsNullOrEmpty(user.LibraryMemberQrImageRelativePath))
            {
                try
                {
                    user.LibraryMemberQrImageRelativePath = await WriteMemberQrAsync(
                        user.LibraryMemberQrToken!,
                        cancellationToken);
                    await _users.UpdateAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Tạo ảnh QR thành viên thất bại cho {UserId}", userId);
                    return ServiceResult<LibraryMemberCardViewModel>.Fail("qr_error", "Không tạo được ảnh QR.");
                }
            }

            var vm = new LibraryMemberCardViewModel
            {
                FullName = user.FullName,
                QrPayload = user.LibraryMemberQrToken!,
                QrImageUrl = user.LibraryMemberQrImageRelativePath
            };
            return ServiceResult<LibraryMemberCardViewModel>.Ok(vm);
        }

        private async Task<string> WriteMemberQrAsync(string token, CancellationToken cancellationToken)
        {
            var folder = Path.Combine(_env.WebRootPath, "images", "qrcodes", "members");
            Directory.CreateDirectory(folder);
            var fileName = $"member-{token}.png";
            var fullPath = Path.Combine(folder, fileName);

            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(8);
            await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);
            return "/images/qrcodes/members/" + fileName;
        }
    }
}
