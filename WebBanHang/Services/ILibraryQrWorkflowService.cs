using WebBanHang.Models.ViewModels;
using WebBanHang.Services.Results;

namespace WebBanHang.Services
{
    public interface ILibraryQrWorkflowService
    {
        Task<ServiceResult<BookCopyLookupViewModel>> LookupCopyAsync(
            string rawPayload,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<int>> BorrowWithMemberAndCopyAsync(
            string memberQrRaw,
            string copyQrRaw,
            CancellationToken cancellationToken = default);

        /// <summary>Trả sách: admin bỏ qua kiểm tra chủ phiếu; thành viên phải khớp UserId.</summary>
        Task<ServiceResult> ReturnWithCopyAsync(
            string copyQrRaw,
            bool asAdmin,
            string? memberUserIdWhenNotAdmin,
            CancellationToken cancellationToken = default);
    }
}
