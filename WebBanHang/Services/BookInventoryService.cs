using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services.Results;
using WebBanHang.Validation;

namespace WebBanHang.Services
{
    public class BookInventoryService : IBookInventoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<BookInventoryService> _logger;

        public BookInventoryService(ApplicationDbContext db, ILogger<BookInventoryService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ServiceResult<int>> StartSessionAsync(
            string adminUserId,
            string? note,
            CancellationToken cancellationToken = default)
        {
            var session = new BookInventorySession
            {
                StartedByUserId = adminUserId,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                StartedAtUtc = DateTime.UtcNow
            };
            _db.BookInventorySessions.Add(session);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Inventory session {SessionId} started by {UserId}.", session.Id, adminUserId);
            return ServiceResult<int>.Ok(session.Id);
        }

        public async Task<ServiceResult<InventoryScanResultViewModel>> RecordScanAsync(
            int sessionId,
            string copyPayload,
            string? observedShelfLocation,
            CancellationToken cancellationToken = default)
        {
            if (!QrPayloadNormalizer.TryParseBookCopyPayload(copyPayload, out var id, out var code))
            {
                return ServiceResult<InventoryScanResultViewModel>.Fail("invalid_qr", "Mã QR sách không hợp lệ.");
            }

            var session = await _db.BookInventorySessions.FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
            if (session == null)
            {
                return ServiceResult<InventoryScanResultViewModel>.Fail("session_not_found", "Không tìm thấy phiên kiểm kê.");
            }

            if (session.CompletedAtUtc != null)
            {
                return ServiceResult<InventoryScanResultViewModel>.Fail("session_closed", "Phiên kiểm kê đã kết thúc.");
            }

            var copy = await ResolveCopyAsync(id, code, track: true, cancellationToken);
            if (copy == null)
            {
                return ServiceResult<InventoryScanResultViewModel>.Fail("copy_not_found", "Không tìm thấy bản sao.");
            }

            var book = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == copy.ProductId, cancellationToken);

            var wrongShelf = false;
            var obs = observedShelfLocation?.Trim();
            var expected = copy.ShelfLocation?.Trim();
            if (!string.IsNullOrEmpty(expected) && !string.IsNullOrEmpty(obs) &&
                !string.Equals(expected, obs, StringComparison.OrdinalIgnoreCase))
            {
                wrongShelf = true;
            }

            var scan = await _db.BookInventoryScans
                .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.BookCopyId == copy.Id, cancellationToken);

            if (scan == null)
            {
                scan = new BookInventoryScan
                {
                    SessionId = sessionId,
                    BookCopyId = copy.Id,
                    ObservedShelfLocation = string.IsNullOrEmpty(obs) ? null : obs,
                    ScannedAtUtc = DateTime.UtcNow
                };
                _db.BookInventoryScans.Add(scan);
            }
            else
            {
                scan.ObservedShelfLocation = string.IsNullOrEmpty(obs) ? scan.ObservedShelfLocation : obs;
                scan.ScannedAtUtc = DateTime.UtcNow;
            }

            copy.LastInventoryVerifiedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            var vm = new InventoryScanResultViewModel
            {
                SessionId = sessionId,
                BookCopyId = copy.Id,
                CopyCode = copy.CopyCode,
                BookTitle = book?.Name ?? "—",
                WrongShelf = wrongShelf,
                Message = wrongShelf ? "Cảnh báo: vị trí quét khác vị trí trên hệ thống." : null
            };

            return ServiceResult<InventoryScanResultViewModel>.Ok(vm);
        }

        public async Task<ServiceResult<InventoryCompleteViewModel>> CompleteSessionAsync(
            int sessionId,
            CancellationToken cancellationToken = default)
        {
            var session = await _db.BookInventorySessions
                .Include(x => x.Scans)
                .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

            if (session == null)
            {
                return ServiceResult<InventoryCompleteViewModel>.Fail("session_not_found", "Không tìm thấy phiên kiểm kê.");
            }

            if (session.CompletedAtUtc != null)
            {
                return ServiceResult<InventoryCompleteViewModel>.Fail("session_closed", "Phiên đã được đóng trước đó.");
            }

            session.CompletedAtUtc = DateTime.UtcNow;

            var allCopies = await _db.BookCopies.AsNoTracking()
                .Select(x => new { x.Id, x.CopyCode })
                .ToListAsync(cancellationToken);

            var scannedIds = session.Scans.Select(s => s.BookCopyId).ToHashSet();
            var missingCodes = allCopies
                .Where(c => !scannedIds.Contains(c.Id))
                .Select(c => c.CopyCode)
                .OrderBy(x => x)
                .ToList();

            var wrongLines = new List<string>();
            var scans = await _db.BookInventoryScans
                .AsNoTracking()
                .Include(x => x.BookCopy)
                .Where(x => x.SessionId == sessionId)
                .ToListAsync(cancellationToken);

            foreach (var s in scans)
            {
                if (s.BookCopy == null)
                {
                    continue;
                }

                var exp = s.BookCopy.ShelfLocation?.Trim();
                var ob = s.ObservedShelfLocation?.Trim();
                if (!string.IsNullOrEmpty(exp) && !string.IsNullOrEmpty(ob) &&
                    !string.Equals(exp, ob, StringComparison.OrdinalIgnoreCase))
                {
                    wrongLines.Add($"{s.BookCopy.CopyCode}: hệ thống '{exp}', quét '{ob}'");
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            var vm = new InventoryCompleteViewModel
            {
                SessionId = sessionId,
                TotalCopiesInLibrary = allCopies.Count,
                ScannedCount = scannedIds.Count,
                MissingCopyCodes = missingCodes,
                WrongShelfLines = wrongLines
            };

            return ServiceResult<InventoryCompleteViewModel>.Ok(vm);
        }

        private async Task<BookCopy?> ResolveCopyAsync(
            int? bookCopyId,
            string? copyCode,
            bool track,
            CancellationToken cancellationToken)
        {
            var q = _db.BookCopies.AsQueryable();
            if (!track)
            {
                q = q.AsNoTracking();
            }

            if (bookCopyId is int bid)
            {
                return await q.FirstOrDefaultAsync(x => x.Id == bid, cancellationToken);
            }

            if (!string.IsNullOrEmpty(copyCode))
            {
                return await q.FirstOrDefaultAsync(x => x.CopyCode == copyCode, cancellationToken);
            }

            return null;
        }
    }
}
