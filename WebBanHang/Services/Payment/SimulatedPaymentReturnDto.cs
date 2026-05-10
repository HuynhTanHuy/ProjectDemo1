namespace WebBanHang.Services.PaymentGateway
{
    public class SimulatedPaymentReturnDto
    {
        public int PaymentId { get; init; }
        public int OrderId { get; init; }
        public decimal Amount { get; init; }
        public string Result { get; init; } = string.Empty;
        public string Signature { get; init; } = string.Empty;
        public string? RawQueryForAudit { get; init; }
    }
}
