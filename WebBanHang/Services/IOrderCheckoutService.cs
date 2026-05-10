using WebBanHang.Models;
using WebBanHang.Services.Results;

namespace WebBanHang.Services
{
    public interface IOrderCheckoutService
    {
        Task<ServiceResult<(int OrderId, int PaymentId)>> CreatePendingOrderWithPaymentAsync(
            string userId,
            ShoppingCart cart,
            string shippingAddress,
            string notes,
            CancellationToken cancellationToken = default);

        /// <summary>Tạo hoặc tái sử dụng bản ghi thanh toán Pending cho đơn chưa thanh toán.</summary>
        Task<ServiceResult<int>> EnsurePendingPaymentAsync(
            string userId,
            int orderId,
            CancellationToken cancellationToken = default);
    }
}
