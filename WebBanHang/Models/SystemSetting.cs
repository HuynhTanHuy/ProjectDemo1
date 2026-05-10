using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    /// <summary>
    /// Bản ghi cấu hình singleton (Id = 1) cho thư viện / mượn trả.
    /// </summary>
    public class SystemSetting
    {
        public const int SingletonId = 1;

        [Key]
        public int Id { get; set; } = SingletonId;

        [Display(Name = "Ngày mượn mặc định")]
        [Range(1, 365)]
        public int DefaultBorrowDays { get; set; } = 14;

        [Display(Name = "Số ngày mượn tối đa")]
        [Range(1, 365)]
        public int MaxBorrowDays { get; set; } = 30;

        [Display(Name = "Số sách mượn tối đa / người")]
        [Range(1, 50)]
        public int MaxBorrowBookPerUser { get; set; } = 5;

        [Display(Name = "Phí mượn (đ)")]
        [Range(0, 100_000_000)]
        public decimal BorrowFee { get; set; }

        [Display(Name = "Phí quá hạn / ngày (đ)")]
        [Range(0, 100_000_000)]
        public decimal OverdueFeePerDay { get; set; } = 5000m;

        /// <summary>Số ngày trước hạn trả để gửi nhắc (0 = tắt nhắc trước hạn).</summary>
        [Display(Name = "Nhắc trước hạn (ngày)")]
        [Range(0, 30)]
        public int RemindBeforeDueDays { get; set; } = 2;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
