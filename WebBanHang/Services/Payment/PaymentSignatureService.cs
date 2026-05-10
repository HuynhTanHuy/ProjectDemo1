using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using WebBanHang.Options;

namespace WebBanHang.Services.PaymentGateway
{
    public class PaymentSignatureService : IPaymentSignatureService
    {
        private readonly byte[] _secret;

        public PaymentSignatureService(IOptions<SimulatedPaymentOptions> options)
        {
            var key = options.Value.SecretKey;
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException(
                    "Cấu hình Payment:Simulated:SecretKey là bắt buộc (xem appsettings).");
            }

            _secret = Encoding.UTF8.GetBytes(key);
        }

        public string Sign(int paymentId, int orderId, decimal amount, string result)
        {
            var payload = BuildPayload(paymentId, orderId, amount, result);
            using var hmac = new HMACSHA256(_secret);
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        public bool Verify(int paymentId, int orderId, decimal amount, string result, string signatureHex)
        {
            if (string.IsNullOrWhiteSpace(signatureHex))
            {
                return false;
            }

            try
            {
                var expected = Sign(paymentId, orderId, amount, result);
                var a = Convert.FromHexString(signatureHex.Trim());
                var b = Convert.FromHexString(expected);
                return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string BuildPayload(int paymentId, int orderId, decimal amount, string result) =>
            string.Create(CultureInfo.InvariantCulture,
                $"{paymentId}|{orderId}|{amount:F2}|{result}");
    }
}
