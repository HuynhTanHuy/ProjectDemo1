using WebBanHang.Models;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services.Results;

namespace WebBanHang.Services
{
    public interface IBorrowService
    {
        Task<ServiceResult> BorrowBookAsync(string userId, int bookId, CancellationToken cancellationToken = default);

        Task<ServiceResult> ReturnBookAsync(string userId, int borrowId, CancellationToken cancellationToken = default);

        Task<ServiceResult> AdminMarkReturnedAsync(int borrowId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CustomerBorrowRowViewModel>> GetActiveBorrowsForUserAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task<PagedResult<CustomerBorrowRowViewModel>> GetBorrowHistoryForUserAsync(
            string userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<CustomerBorrowDetailViewModel?> GetBorrowDetailForUserAsync(
            string userId,
            int borrowId,
            CancellationToken cancellationToken = default);
    }
}
