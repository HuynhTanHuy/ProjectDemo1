using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Repositories
{
    public class EFBookCopyRepository : IBookCopyRepository
    {
        private readonly ApplicationDbContext _db;

        public EFBookCopyRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<BookCopy?> GetByIdAsync(int id, bool track, CancellationToken cancellationToken = default)
        {
            var q = _db.BookCopies.AsQueryable();
            if (!track)
            {
                q = q.AsNoTracking();
            }

            return q.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<BookCopy?> GetByCopyCodeAsync(string copyCode, bool track, CancellationToken cancellationToken = default)
        {
            var normalized = copyCode.Trim().ToUpperInvariant();
            var q = _db.BookCopies.AsQueryable();
            if (!track)
            {
                q = q.AsNoTracking();
            }

            return q.FirstOrDefaultAsync(x => x.CopyCode == normalized, cancellationToken);
        }

        public Task<int> CountForProductAsync(int productId, CancellationToken cancellationToken = default) =>
            _db.BookCopies.CountAsync(x => x.ProductId == productId, cancellationToken);

        public async Task<IReadOnlyList<BookCopy>> ListRemovableCopiesAsync(
            int productId,
            int take,
            CancellationToken cancellationToken = default)
        {
            var borrowedIds = await _db.Borrows
                .AsNoTracking()
                .Where(x =>
                    x.BookCopyId != null &&
                    (x.Status == BorrowStatus.Borrowing || x.Status == BorrowStatus.Overdue))
                .Select(x => x.BookCopyId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            return await _db.BookCopies
                .Where(x =>
                    x.ProductId == productId &&
                    x.Status == BookCopyStatus.Active &&
                    !borrowedIds.Contains(x.Id))
                .OrderByDescending(x => x.Id)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<BookCopy?> FirstAvailableCopyForProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            var borrowedIds = await _db.Borrows
                .AsNoTracking()
                .Where(x =>
                    x.BookCopyId != null &&
                    (x.Status == BorrowStatus.Borrowing || x.Status == BorrowStatus.Overdue))
                .Select(x => x.BookCopyId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            return await _db.BookCopies
                .Where(x =>
                    x.ProductId == productId &&
                    x.Status == BookCopyStatus.Active &&
                    !borrowedIds.Contains(x.Id))
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(BookCopy copy, CancellationToken cancellationToken = default)
        {
            _db.BookCopies.Add(copy);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _db.SaveChangesAsync(cancellationToken);

        public void Remove(BookCopy copy) => _db.BookCopies.Remove(copy);

        public async Task<IReadOnlyList<int>> GetAllCopyIdsAsync(CancellationToken cancellationToken = default)
        {
            var list = await _db.BookCopies.AsNoTracking().Select(x => x.Id).ToListAsync(cancellationToken);
            return list;
        }
    }
}
