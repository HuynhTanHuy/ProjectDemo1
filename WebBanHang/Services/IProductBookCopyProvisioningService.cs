namespace WebBanHang.Services
{
    /// <summary>Đồng bộ số bản sao vật lý với Stock + phiếu mượn đang hoạt động.</summary>
    public interface IProductBookCopyProvisioningService
    {
        Task SyncProductCopiesAsync(int productId, CancellationToken cancellationToken = default);

        Task<int> SyncAllProductsAsync(CancellationToken cancellationToken = default);
    }
}
