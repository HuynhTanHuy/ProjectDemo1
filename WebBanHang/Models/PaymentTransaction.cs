using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebBanHang.Models
{
    /// <summary>
    /// Nhật ký sự kiện thanh toán (callback, xác minh, cập nhật đơn) — hỗ trợ audit và idempotent.
    /// </summary>
    public class PaymentTransaction
    {
        public long Id { get; set; }

        public int PaymentId { get; set; }

        [Required]
        [StringLength(80)]
        public string EventType { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? PayloadSnapshot { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [StringLength(128)]
        public string? ExternalReference { get; set; }

        [ForeignKey("PaymentId")]
        [ValidateNever]
        public Payment Payment { get; set; } = null!;
    }
}
