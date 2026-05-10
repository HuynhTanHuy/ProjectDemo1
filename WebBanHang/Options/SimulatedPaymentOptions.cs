namespace WebBanHang.Options
{
    public class SimulatedPaymentOptions
    {
        public const string SectionName = "Payment:Simulated";

        /// <summary>Chuỗi bí mật ký HMAC cho callback (phải cấu hình trên môi trường thật).</summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>Tên provider lưu trong DB (audit).</summary>
        public string ProviderName { get; set; } = "Simulated";
    }
}
