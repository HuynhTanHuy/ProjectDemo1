namespace WebBanHang.Services.PaymentGateway
{
    public sealed class PaymentCallbackResult
    {
        public string Code { get; init; } = string.Empty;
        public int OrderId { get; init; }
        public int PaymentId { get; init; }
        public string? Message { get; init; }
    }
}
