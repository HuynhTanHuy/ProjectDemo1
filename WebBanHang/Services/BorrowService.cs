using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services.Results;

namespace WebBanHang.Services
{
    public class BorrowService : IBorrowService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISystemSettingsService _settings;
        private readonly ILogger<BorrowService> _logger;

        public BorrowService(
            ApplicationDbContext db,
            ISystemSettingsService settings,
            ILogger<BorrowService> logger)
        {
            _db = db;
            _settings = settings;
            _logger = logger;
        }

        public async Task<ServiceResult> BorrowBookAsync(string userId, int bookId, CancellationToken cancellationToken = default)
        {
            var settings = await _settings.GetAsync(cancellationToken);
            var borrowDays = Math.Clamp(settings.DefaultBorrowDays, 1, settings.MaxBorrowDays);
            var utcNow = DateTime.UtcNow;

            var hasUnpaidPenalty = await _db.Penalties.AnyAsync(x => x.UserId == userId && !x.IsPaid, cancellationToken);
            if (hasUnpaidPenalty)
            {
                return ServiceResult.Fail("unpaid_penalty", "Bạn còn khoản phạt chưa thanh toán. Vui lòng xử lý trước khi mượn sách.");
            }

            if (await HasBorrowOverdueBlockAsync(userId, utcNow, cancellationToken))
            {
                return ServiceResult.Fail("overdue_block", "Bạn đang có sách quá hạn. Vui lòng trả sách và thanh toán phạt trước khi mượn thêm.");
            }

            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var book = await _db.Products.FirstOrDefaultAsync(x => x.Id == bookId, cancellationToken);
                if (book == null)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return ServiceResult.Fail("book_not_found", "Không tìm thấy sách.");
                }

                if (book.Stock <= 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return ServiceResult.Fail("out_of_stock", "Sách đã hết trong kho.");
                }

                var activeCount = await _db.Borrows.CountAsync(x =>
                    x.UserId == userId &&
                    (x.Status == BorrowStatus.Borrowing || x.Status == BorrowStatus.Overdue), cancellationToken);

                if (activeCount >= settings.MaxBorrowBookPerUser)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return ServiceResult.Fail("borrow_limit", $"Bạn chỉ được mượn tối đa {settings.MaxBorrowBookPerUser} cuốn cùng lúc.");
                }

                var duplicate = await _db.Borrows.AnyAsync(x =>
                    x.UserId == userId &&
                    x.BookId == bookId &&
                    (x.Status == BorrowStatus.Borrowing || x.Status == BorrowStatus.Overdue), cancellationToken);

                if (duplicate)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return ServiceResult.Fail("duplicate_borrow", "Bạn đang mượn cuốn sách này.");
                }

                book.Stock -= 1;
                _db.Borrows.Add(new Borrow
                {
                    UserId = userId,
                    BookId = bookId,
                    BorrowDate = utcNow,
                    DueDate = utcNow.Date.AddDays(borrowDays),
                    Status = BorrowStatus.Borrowing,
                    BorrowFeeAmount = settings.BorrowFee,
                    FineAmount = 0,
                    OverdueDays = 0
                });

                await _db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                _logger.LogInformation("User {UserId} borrowed book {BookId}.", userId, bookId);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "BorrowBook failed for user {UserId} book {BookId}", userId, bookId);
                return ServiceResult.Fail("server_error", "Không thể tạo phiếu mượn. Vui lòng thử lại.");
            }
        }

        public Task<ServiceResult> ReturnBookAsync(string userId, int borrowId, CancellationToken cancellationToken = default) =>
            CompleteReturnAsync(borrowId, userId, cancellationToken);

        public Task<ServiceResult> AdminMarkReturnedAsync(int borrowId, CancellationToken cancellationToken = default) =>
            CompleteReturnAsync(borrowId, userId: null, cancellationToken);

        private async Task<ServiceResult> CompleteReturnAsync(int borrowId, string? userId, CancellationToken cancellationToken)
        {
            var settings = await _settings.GetAsync(cancellationToken);
            var utcNow = DateTime.UtcNow;

            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var borrow = await _db.Borrows
                    .Include(x => x.Book)
                    .FirstOrDefaultAsync(x => x.Id == borrowId, cancellationToken);

                if (borrow == null)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return ServiceResult.Fail("not_found", "Không tìm thấy phiếu mượn.");
                }

                if (userId != null && borrow.UserId != userId)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return ServiceResult.Fail("forbidden", "Bạn không có quyền thao tác trên phiếu này.");
                }

                if (borrow.Status is BorrowStatus.Returned or BorrowStatus.Cancelled)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return ServiceResult.Fail("already_closed", "Phiếu mượn đã được xử lý trước đó.");
                }

                if (borrow.Status == BorrowStatus.Lost)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return ServiceResult.Fail("lost", "Sách đang ở trạng thái mất, không thể trả qua luồng thông thường.");
                }

                borrow.ReturnDate = utcNow;
                borrow.Status = BorrowStatus.Returned;

                if (borrow.Book != null)
                {
                    borrow.Book.Stock += 1;
                }

                var penaltyAmount = borrow.FineAmount;
                if (penaltyAmount <= 0 && borrow.ReturnDate.Value.Date > borrow.DueDate.Date)
                {
                    var lateDays = (borrow.ReturnDate.Value.Date - borrow.DueDate.Date).Days;
                    penaltyAmount = lateDays * settings.OverdueFeePerDay;
                }

                var hasUnpaidForBorrow = await _db.Penalties.AnyAsync(
                    p => p.BorrowId == borrow.Id && !p.IsPaid,
                    cancellationToken);

                if (penaltyAmount > 0 && !hasUnpaidForBorrow)
                {
                    _db.Penalties.Add(new Penalty
                    {
                        UserId = borrow.UserId,
                        BorrowId = borrow.Id,
                        Amount = penaltyAmount,
                        Reason = borrow.FineAmount > 0
                            ? $"Quá hạn ({borrow.OverdueDays} ngày), phạt tích lũy."
                            : $"Trả trễ ({(borrow.ReturnDate!.Value.Date - borrow.DueDate.Date).Days} ngày).",
                        CreatedAt = utcNow,
                        IsPaid = false
                    });
                }

                borrow.FineAmount = 0;
                borrow.OverdueDays = 0;

                await _db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                _logger.LogInformation("Borrow {BorrowId} returned.", borrowId);
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Return borrow {BorrowId} failed", borrowId);
                return ServiceResult.Fail("server_error", "Không thể ghi nhận trả sách.");
            }
        }

        public async Task<IReadOnlyList<CustomerBorrowRowViewModel>> GetActiveBorrowsForUserAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var list = await _db.Borrows
                .AsNoTracking()
                .Include(x => x.Book)
                .Where(x => x.UserId == userId &&
                            (x.Status == BorrowStatus.Borrowing || x.Status == BorrowStatus.Overdue))
                .OrderByDescending(x => x.BorrowDate)
                .ToListAsync(cancellationToken);

            return list.Select(b => MapRow(b, utcNow)).ToList();
        }

        public async Task<PagedResult<CustomerBorrowRowViewModel>> GetBorrowHistoryForUserAsync(
            string userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var utcNow = DateTime.UtcNow;

            var query = _db.Borrows
                .AsNoTracking()
                .Include(x => x.Book)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.BorrowDate);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<CustomerBorrowRowViewModel>
            {
                Items = items.Select(b => MapRow(b, utcNow)).ToList(),
                TotalItems = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<CustomerBorrowDetailViewModel?> GetBorrowDetailForUserAsync(
            string userId,
            int borrowId,
            CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var borrow = await _db.Borrows
                .AsNoTracking()
                .Include(x => x.Book)
                .ThenInclude(b => b!.Category)
                .FirstOrDefaultAsync(x => x.Id == borrowId && x.UserId == userId, cancellationToken);

            if (borrow == null)
            {
                return null;
            }

            var row = MapRow(borrow, utcNow);
            return new CustomerBorrowDetailViewModel
            {
                BorrowId = row.BorrowId,
                BookId = row.BookId,
                BookTitle = row.BookTitle,
                BorrowDateUtc = row.BorrowDateUtc,
                DueDateUtc = row.DueDateUtc,
                ReturnDateUtc = row.ReturnDateUtc,
                Status = row.Status,
                DaysRemaining = row.DaysRemaining,
                BorrowFeeAmount = row.BorrowFeeAmount,
                FineAmount = row.FineAmount,
                OverdueDays = row.OverdueDays,
                CategoryName = borrow.Book?.Category?.Name,
                BookImageUrl = borrow.Book?.ImageUrl
            };
        }

        private static CustomerBorrowRowViewModel MapRow(Borrow b, DateTime utcNow)
        {
            return new CustomerBorrowRowViewModel
            {
                BorrowId = b.Id,
                BookId = b.BookId,
                BookTitle = b.Book?.Name ?? "—",
                BorrowDateUtc = b.BorrowDate,
                DueDateUtc = b.DueDate,
                ReturnDateUtc = b.ReturnDate,
                Status = b.Status,
                DaysRemaining = GetDaysRemaining(b, utcNow),
                BorrowFeeAmount = b.BorrowFeeAmount,
                FineAmount = b.FineAmount,
                OverdueDays = b.OverdueDays
            };
        }

        private static int? GetDaysRemaining(Borrow b, DateTime utcNow)
        {
            if (b.Status is BorrowStatus.Returned or BorrowStatus.Cancelled or BorrowStatus.Lost)
            {
                return null;
            }

            return (b.DueDate.Date - utcNow.Date).Days;
        }

        private async Task<bool> HasBorrowOverdueBlockAsync(string userId, DateTime utcNow, CancellationToken cancellationToken)
        {
            return await _db.Borrows.AnyAsync(x =>
                    x.UserId == userId &&
                    (x.Status == BorrowStatus.Overdue ||
                     (x.Status == BorrowStatus.Borrowing && x.DueDate.Date < utcNow.Date)),
                cancellationToken);
        }
    }
}
