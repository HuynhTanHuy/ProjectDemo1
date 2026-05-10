using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Repositories;

namespace WebBanHang.Services
{
    public class ProductBookCopyProvisioningService : IProductBookCopyProvisioningService
    {
        private readonly ApplicationDbContext _db;
        private readonly IBookCopyRepository _copies;
        private readonly IBookCopyQrService _qr;
        private readonly ILogger<ProductBookCopyProvisioningService> _logger;

        public ProductBookCopyProvisioningService(
            ApplicationDbContext db,
            IBookCopyRepository copies,
            IBookCopyQrService qr,
            ILogger<ProductBookCopyProvisioningService> logger)
        {
            _db = db;
            _copies = copies;
            _qr = qr;
            _logger = logger;
        }

        public async Task SyncProductCopiesAsync(int productId, CancellationToken cancellationToken = default)
        {
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);
            if (product == null)
            {
                return;
            }

            var activeBorrowCount = await _db.Borrows.CountAsync(
                x =>
                    x.BookId == productId &&
                    (x.Status == BorrowStatus.Borrowing || x.Status == BorrowStatus.Overdue),
                cancellationToken);

            var target = product.Stock + activeBorrowCount;
            var current = await _copies.CountForProductAsync(productId, cancellationToken);

            while (current < target)
            {
                var pending = $"PEND-{Guid.NewGuid():N}";
                var copy = new BookCopy
                {
                    ProductId = productId,
                    CopyCode = pending,
                    QrPayload = pending,
                    CreatedAtUtc = DateTime.UtcNow
                };
                await _copies.AddAsync(copy, cancellationToken);
                await _qr.FinalizeNewBookCopyAsync(copy.Id, cancellationToken);
                current++;
                _logger.LogInformation("Đã tạo BookCopy {CopyId} cho sách {ProductId}.", copy.Id, productId);
            }

            while (current > target)
            {
                var removeCount = current - target;
                var victims = await _copies.ListRemovableCopiesAsync(productId, removeCount, cancellationToken);
                if (victims.Count == 0)
                {
                    _logger.LogWarning(
                        "Không thể giảm bản sao sách {ProductId}: còn bản đang mượn hoặc không đủ bản khả dụng.",
                        productId);
                    break;
                }

                foreach (var v in victims)
                {
                    _qr.TryDeleteQrFile(v.QrImageRelativePath);
                    _copies.Remove(v);
                    current--;
                }

                await _copies.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<int> SyncAllProductsAsync(CancellationToken cancellationToken = default)
        {
            var ids = await _db.Products.AsNoTracking().Select(x => x.Id).ToListAsync(cancellationToken);
            foreach (var id in ids)
            {
                await SyncProductCopiesAsync(id, cancellationToken);
            }

            return ids.Count;
        }
    }
}
