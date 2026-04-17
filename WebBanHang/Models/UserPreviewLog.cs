using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class UserPreviewLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [StringLength(450)]
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        [Required]
        public int BookId { get; set; }

        public Product? Book { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        public int DurationSeconds { get; set; }
    }
}
