using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    /// <summary>Bản sao vật lý của một đầu sách (Product); mỗi bản có QR riêng.</summary>
    public class BookCopy
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public Product? Book { get; set; }

        /// <summary>Mã hiển thị và là nội dung QR (ví dụ COPY-000042).</summary>
        [Required, MaxLength(32)]
        public string CopyCode { get; set; } = string.Empty;

        /// <summary>Giá trị ghi trong QR — trùng CopyCode, không chứa JSON.</summary>
        [Required, MaxLength(32)]
        public string QrPayload { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? QrImageRelativePath { get; set; }

        [MaxLength(120)]
        public string? ShelfLocation { get; set; }

        public BookCopyStatus Status { get; set; } = BookCopyStatus.Active;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastInventoryVerifiedAtUtc { get; set; }
    }
}
