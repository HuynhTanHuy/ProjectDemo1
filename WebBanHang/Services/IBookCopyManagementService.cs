using WebBanHang.Models.ViewModels;
using WebBanHang.Services.Results;

namespace WebBanHang.Services
{
    public interface IBookCopyManagementService
    {
        Task<BookCopyIndexViewModel> GetIndexAsync(
            BookCopyIndexViewModel filter,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> UpdateShelfLocationAsync(
            int bookCopyId,
            string shelfLocation,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> MarkLostAsync(
            int bookCopyId,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> MarkDisposedAsync(
            int bookCopyId,
            CancellationToken cancellationToken = default);
    }
}
