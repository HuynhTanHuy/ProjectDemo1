namespace WebBanHang.Services.PaymentGateway
{
    public interface IPaymentSignatureService
    {
        string Sign(int paymentId, int orderId, decimal amount, string result);

        bool Verify(int paymentId, int orderId, decimal amount, string result, string signatureHex);
    }
}
