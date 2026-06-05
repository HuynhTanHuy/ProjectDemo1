using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services.Results;

namespace WebBanHang.Services
{
    public class BookCopyManagementService : IBookCopyManagementService
    {
        private readonly ApplicationDbContext _db;

        public BookCopyManagementService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<BookCopyIndexViewModel> GetIndexAsync(
            BookCopyIndexViewModel filter,
            CancellationToken cancellationToken = default)
        {
            filter.PageNumber = Math.Max(1, filter.PageNumber);
            filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

            var query = _db.BookCopies
                .AsNoTracking()
                .Include(x => x.Book)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var s = filter.SearchQuery.Trim();
                query = query.Where(x =>
                    x.CopyCode.Contains(s) ||
                    (x.Book != null && x.Book.Name.Contains(s)));
            }

            if (filter.PhysicalStatus.HasValue)
            {
                query = query.Where(x => x.Status == filter.PhysicalStatus.Value);
            }

            query = query.OrderByDescending(x => x.Id);

            filter.TotalCount = await query.CountAsync(cancellationToken);
            var copies = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            var copyIds = copies.Select(x => x.Id).ToList();
            var activeBorrows = await _db.Borrows
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x =>
                    x.BookCopyId != null &&
                    copyIds.Contains(x.BookCopyId.Value) &&
                    (x.Status == BorrowStatus.Borrowing || x.Status == BorrowStatus.Overdue))
                .ToListAsync(cancellationToken);

            var borrowByCopyId = activeBorrows
                .GroupBy(x => x.BookCopyId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.BorrowDate).First());

            filter.Items = copies.Select(copy =>
            {
                borrowByCopyId.TryGetValue(copy.Id, out var active);
                return new BookCopyListItemViewModel
                {
                    BookCopyId = copy.Id,
                    CopyCode = copy.CopyCode,
                    BookTitle = copy.Book?.Name ?? "—",
                    QrImageRelativeUrl = copy.QrImageRelativePath,
                    ShelfLocation = copy.ShelfLocation,
                    PhysicalStatus = copy.Status,
                    BorrowStatusText = BuildBorrowStatusText(copy.Status, active),
                    BorrowedByUserName = active?.User?.UserName,
                    BorrowedByFullName = active?.User?.FullName
                };
            }).ToList();

            filter.PhysicalStatusOptions = BuildPhysicalStatusOptions(filter.PhysicalStatus);
            return filter;
        }

        public async Task<ServiceResult> UpdateShelfLocationAsync(
            int bookCopyId,
            string shelfLocation,
            CancellationToken cancellationToken = default)
        {
            var normalized = shelfLocation.Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                return ServiceResult.Fail("invalid_shelf", "Vị trí kệ không được để trống.");
            }

            if (normalized.Length > 120)
            {
                return ServiceResult.Fail("invalid_shelf", "Vị trí kệ tối đa 120 ký tự.");
            }

            var copy = await _db.BookCopies.FirstOrDefaultAsync(x => x.Id == bookCopyId, cancellationToken);
            if (copy == null)
            {
                return ServiceResult.Fail("copy_not_found", "Không tìm thấy bản sao.");
            }

            copy.ShelfLocation = normalized.ToUpperInvariant();
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> MarkLostAsync(
            int bookCopyId,
            CancellationToken cancellationToken = default)
        {
            return await SetPhysicalStatusAsync(bookCopyId, BookCopyStatus.Lost, cancellationToken);
        }

        public async Task<ServiceResult> MarkDisposedAsync(
            int bookCopyId,
            CancellationToken cancellationToken = default)
        {
            return await SetPhysicalStatusAsync(bookCopyId, BookCopyStatus.Disposed, cancellationToken);
        }

        private async Task<ServiceResult> SetPhysicalStatusAsync(
            int bookCopyId,
            BookCopyStatus targetStatus,
            CancellationToken cancellationToken)
        {
            var copy = await _db.BookCopies.FirstOrDefaultAsync(x => x.Id == bookCopyId, cancellationToken);
            if (copy == null)
            {
                return ServiceResult.Fail("copy_not_found", "Không tìm thấy bản sao.");
            }

            if (copy.Status == targetStatus)
            {
                return ServiceResult.Fail("already_set", "Bản sao đã ở trạng thái này.");
            }

            if (copy.Status != BookCopyStatus.Active)
            {
                return ServiceResult.Fail("invalid_status", "Chỉ có thể đánh dấu từ trạng thái đang sử dụng.");
            }

            var hasActiveBorrow = await _db.Borrows.AnyAsync(
                x =>
                    x.BookCopyId == bookCopyId &&
                    (x.Status == BorrowStatus.Borrowing || x.Status == BorrowStatus.Overdue),
                cancellationToken);

            if (hasActiveBorrow)
            {
                return ServiceResult.Fail("copy_borrowed", "Bản sao đang được mượn. Vui lòng ghi nhận trả trước.");
            }

            copy.Status = targetStatus;
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok();
        }

        internal static string BuildBorrowStatusText(BookCopyStatus physicalStatus, Borrow? active)
        {
            if (physicalStatus == BookCopyStatus.Lost)
            {
                return "Mất";
            }

            if (physicalStatus == BookCopyStatus.Disposed)
            {
                return "Thanh lý";
            }

            if (active == null)
            {
                return "Trong kho";
            }

            return active.Status == BorrowStatus.Overdue
                ? "Đang mượn (quá hạn)"
                : "Đang mượn";
        }

        private static List<SelectListItem> BuildPhysicalStatusOptions(BookCopyStatus? selected)
        {
            return
            [
                new SelectListItem { Value = "", Text = "Tất cả trạng thái", Selected = !selected.HasValue },
                new SelectListItem
                {
                    Value = ((int)BookCopyStatus.Active).ToString(),
                    Text = "Đang sử dụng",
                    Selected = selected == BookCopyStatus.Active
                },
                new SelectListItem
                {
                    Value = ((int)BookCopyStatus.Lost).ToString(),
                    Text = "Mất",
                    Selected = selected == BookCopyStatus.Lost
                },
                new SelectListItem
                {
                    Value = ((int)BookCopyStatus.Disposed).ToString(),
                    Text = "Thanh lý",
                    Selected = selected == BookCopyStatus.Disposed
                }
            ];
        }
    }
}
