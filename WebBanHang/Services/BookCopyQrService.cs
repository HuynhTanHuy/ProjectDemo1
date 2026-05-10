using Microsoft.EntityFrameworkCore;
using QRCoder;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class BookCopyQrService : IBookCopyQrService
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<BookCopyQrService> _logger;

        public BookCopyQrService(
            ApplicationDbContext db,
            IWebHostEnvironment env,
            ILogger<BookCopyQrService> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }

        public async Task FinalizeNewBookCopyAsync(int bookCopyId, CancellationToken cancellationToken = default)
        {
            var copy = await _db.BookCopies.FirstOrDefaultAsync(x => x.Id == bookCopyId, cancellationToken)
                ?? throw new InvalidOperationException("BookCopy not found: " + bookCopyId);

            copy.CopyCode = $"COPY-{copy.Id:D6}";
            copy.QrPayload = copy.CopyCode;
            copy.QrImageRelativePath = await WritePngAsync(copy.QrPayload, $"bookcopy-{copy.Id}.png", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task RegenerateBookCopyQrAsync(int bookCopyId, CancellationToken cancellationToken = default)
        {
            var copy = await _db.BookCopies.FirstOrDefaultAsync(x => x.Id == bookCopyId, cancellationToken)
                ?? throw new InvalidOperationException("BookCopy not found: " + bookCopyId);

            TryDeleteQrFile(copy.QrImageRelativePath);
            if (string.IsNullOrWhiteSpace(copy.QrPayload))
            {
                copy.QrPayload = copy.CopyCode;
            }

            copy.QrImageRelativePath = await WritePngAsync(copy.QrPayload, $"bookcopy-{copy.Id}.png", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public void TryDeleteQrFile(string? webRelativePath)
        {
            if (string.IsNullOrWhiteSpace(webRelativePath))
            {
                return;
            }

            try
            {
                var trimmed = webRelativePath.TrimStart('~', '/', '\\');
                var physical = Path.Combine(_env.WebRootPath, trimmed.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(physical))
                {
                    File.Delete(physical);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không xóa được file QR: {Path}", webRelativePath);
            }
        }

        private async Task<string> WritePngAsync(string payload, string fileName, CancellationToken cancellationToken)
        {
            var folder = Path.Combine(_env.WebRootPath, "images", "qrcodes", "bookcopies");
            Directory.CreateDirectory(folder);
            var fullPath = Path.Combine(folder, fileName);

            var pngBytes = RenderPng(payload);
            await File.WriteAllBytesAsync(fullPath, pngBytes, cancellationToken);

            return "/images/qrcodes/bookcopies/" + fileName;
        }

        private static byte[] RenderPng(string payload)
        {
            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            return png.GetGraphic(pixelsPerModule: 8);
        }
    }
}
