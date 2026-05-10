using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebBanHang.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [Range(0.01, 100_000_000)]
        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.SimulatedGateway;

        [Required]
        [StringLength(64)]
        public string TransactionCode { get; set; } = string.Empty;

        [StringLength(64)]
        public string? GatewayProvider { get; set; }

        [StringLength(128)]
        public string? ExternalTransactionId { get; set; }

        [Required]
        [StringLength(128)]
        public string IdempotencyKey { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAtUtc { get; set; }

        [StringLength(500)]
        public string? FailureReason { get; set; }

        [ForeignKey("OrderId")]
        [ValidateNever]
        public Order Order { get; set; } = null!;

        [ValidateNever]
        public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    }
}
