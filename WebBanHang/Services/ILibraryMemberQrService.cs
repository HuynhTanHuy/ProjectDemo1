using WebBanHang.Models.ViewModels;
using WebBanHang.Services.Results;

namespace WebBanHang.Services
{
    public interface ILibraryMemberQrService
    {
        Task<ServiceResult<LibraryMemberCardViewModel>> EnsureMemberCardAsync(
            string userId,
            CancellationToken cancellationToken = default);
    }
}
