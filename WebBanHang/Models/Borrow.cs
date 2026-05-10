using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public enum BorrowStatus
    {
        Borrowing = 1,
        Returned = 2,
        Overdue = 3,
        Lost = 4,
        Cancelled = 5
    }

    public class Borrow
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int BookId { get; set; }

        public DateTime BorrowDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        public BorrowStatus Status { get; set; } = BorrowStatus.Borrowing;

        /// <summary>Phí mượn tại thời điểm tạo phiếu (theo cấu hình hệ thống).</summary>
        [Range(0, 100_000_000)]
        public decimal BorrowFeeAmount { get; set; }

        /// <summary>Tiền phạt quá hạn tích lũy (cập nhật bởi job / khi trả).</summary>
        [Range(0, 100_000_000)]
        public decimal FineAmount { get; set; }

        [Range(0, 3650)]
        public int OverdueDays { get; set; }

        public DateTime? LastDueReminderSentAtUtc { get; set; }

        public DateTime? LastOverdueReminderSentAtUtc { get; set; }

        public ApplicationUser? User { get; set; }
        public Product? Book { get; set; }
    }
}
