using WebBanHang.Models.ViewModels;
using WebBanHang.Services.Results;

namespace WebBanHang.Services
{
    public interface IBookInventoryService
    {
        Task<ServiceResult<int>> StartSessionAsync(
            string adminUserId,
            string? note,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<InventoryScanResultViewModel>> RecordScanAsync(
            int sessionId,
            string copyPayload,
            string? observedShelfLocation,
            CancellationToken cancellationToken = default);

        Task<ServiceResult<InventoryCompleteViewModel>> CompleteSessionAsync(
            int sessionId,
            CancellationToken cancellationToken = default);
    }
}
