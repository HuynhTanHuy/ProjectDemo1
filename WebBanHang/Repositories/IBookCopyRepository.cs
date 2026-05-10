using WebBanHang.Models;

namespace WebBanHang.Repositories
{
    public interface IBookCopyRepository
    {
        Task<BookCopy?> GetByIdAsync(int id, bool track, CancellationToken cancellationToken = default);

        Task<BookCopy?> GetByCopyCodeAsync(string copyCode, bool track, CancellationToken cancellationToken = default);

        Task<int> CountForProductAsync(int productId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BookCopy>> ListRemovableCopiesAsync(int productId, int take, CancellationToken cancellationToken = default);

        Task<BookCopy?> FirstAvailableCopyForProductAsync(int productId, CancellationToken cancellationToken = default);

        Task AddAsync(BookCopy copy, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        void Remove(BookCopy copy);

        Task<IReadOnlyList<int>> GetAllCopyIdsAsync(CancellationToken cancellationToken = default);
    }
}
