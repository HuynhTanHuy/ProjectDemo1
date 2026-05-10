using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services.Results;
using WebBanHang.Validation;

namespace WebBanHang.Services
{
    public class LibraryQrWorkflowService : ILibraryQrWorkflowService
    {
        private readonly ApplicationDbContext _db;
        private readonly IBorrowService _borrow;
        private readonly ILogger<LibraryQrWorkflowService> _logger;

        public LibraryQrWorkflowService(
            ApplicationDbContext db,
            IBorrowService borrow,
            ILogger<LibraryQrWorkflowService> logger)
        {
            _db = db;
            _borrow = borrow;
            _logger = logger;
        }

        public async Task<ServiceResult<BookCopyLookupViewModel>> LookupCopyAsync(
            string rawPayload,
            CancellationToken cancellationToken = default)
        {
            if (!QrPayloadNormalizer.TryParseBookCopyPayload(rawPayload, out var id, out var code))
            {
                return ServiceResult<BookCopyLookupViewModel>.Fail("invalid_qr", "Mã QR không hợp lệ.");
            }

            var copy = await ResolveCopyTrackedAsync(id, code, cancellationToken);
            if (copy == null)
            {
                return ServiceResult<BookCopyLookupViewModel>.Fail("copy_not_found", "Không tìm thấy bản sao.");
            }

            var book = await _db.Products
                .AsNoTracking()
                .Include(x => x.Author)
                .FirstOrDefaultAsync(x => x.Id == copy.ProductId, cancellationToken);

            var active = await _db.Borrows
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x =>
                    x.BookCopyId == copy.Id &&
                    (x.Status == BorrowStatus.Borrowing || x.Status == BorrowStatus.Overdue))
                .OrderByDescending(x => x.BorrowDate)
                .FirstOrDefaultAsync(cancellationToken);

            var vm = new BookCopyLookupViewModel
            {
                BookCopyId = copy.Id,
                CopyCode = copy.CopyCode,
                QrImageRelativeUrl = copy.QrImageRelativePath,
                BookId = copy.ProductId,
                BookTitle = book?.Name ?? "—",
                AuthorName = book?.Author?.Name,
                ShelfLocation = copy.ShelfLocation,
                BorrowedByUserName = active?.User?.UserName,
                BorrowedByFullName = active?.User?.FullName,
                DueDateUtc = active?.DueDate,
                ActiveBorrowStatus = active?.Status,
                CopyStatus = active == null
                    ? "Trong kho"
                    : active.Status == BorrowStatus.Overdue
                        ? "Đang mượn (quá hạn)"
                        : "Đang mượn"
            };

            return ServiceResult<BookCopyLookupViewModel>.Ok(vm);
        }

        public async Task<ServiceResult<int>> BorrowWithMemberAndCopyAsync(
            string memberQrRaw,
            string copyQrRaw,
            CancellationToken cancellationToken = default)
        {
            if (!QrPayloadNormalizer.TryParseMemberToken(memberQrRaw, out var token))
            {
                return ServiceResult<int>.Fail("invalid_member_qr", "Mã QR thành viên không hợp lệ.");
            }

            var memberId = await _db.Users.AsNoTracking()
                .Where(x => x.LibraryMemberQrToken == token)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrEmpty(memberId))
            {
                return ServiceResult<int>.Fail("member_not_found", "Không tìm thấy thành viên cho mã QR này.");
            }

            var result = await _borrow.BorrowBookWithCopyPayloadAsync(memberId, copyQrRaw, cancellationToken);
            if (result.Success)
            {
                _logger.LogInformation("QR borrow created borrow {BorrowId} for member {MemberId}.", result.Data, memberId);
            }

            return result;
        }

        public Task<ServiceResult> ReturnWithCopyAsync(
            string copyQrRaw,
            bool asAdmin,
            string? memberUserIdWhenNotAdmin,
            CancellationToken cancellationToken = default) =>
            asAdmin
                ? _borrow.AdminReturnBookWithCopyPayloadAsync(copyQrRaw, cancellationToken)
                : _borrow.ReturnBookWithCopyPayloadAsync(memberUserIdWhenNotAdmin ?? string.Empty, copyQrRaw, cancellationToken);

        private async Task<BookCopy?> ResolveCopyTrackedAsync(int? bookCopyId, string? copyCode, CancellationToken cancellationToken)
        {
            if (bookCopyId is int bid)
            {
                return await _db.BookCopies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bid, cancellationToken);
            }

            if (!string.IsNullOrEmpty(copyCode))
            {
                return await _db.BookCopies.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.CopyCode == copyCode, cancellationToken);
            }

            return null;
        }
    }
}
