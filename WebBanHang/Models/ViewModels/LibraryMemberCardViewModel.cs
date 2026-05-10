namespace WebBanHang.Models.ViewModels
{
    public class LibraryMemberCardViewModel
    {
        public string FullName { get; set; } = string.Empty;

        /// <summary>Nội dung in trên QR (token opaque).</summary>
        public string QrPayload { get; set; } = string.Empty;

        public string? QrImageUrl { get; set; }
    }
}
