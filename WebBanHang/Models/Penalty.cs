using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class Penalty
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int BorrowId { get; set; }

        [Range(0, 100000000)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }

        public ApplicationUser? User { get; set; }
        public Borrow? Borrow { get; set; }
    }
}
