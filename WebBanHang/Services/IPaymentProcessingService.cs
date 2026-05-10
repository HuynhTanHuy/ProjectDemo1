using WebBanHang.Services.PaymentGateway;

namespace WebBanHang.Services
{
    public interface IPaymentProcessingService
    {
        Task<PaymentCallbackResult> ProcessSimulatedReturnAsync(
            SimulatedPaymentReturnDto dto,
            CancellationToken cancellationToken = default);
    }
}
