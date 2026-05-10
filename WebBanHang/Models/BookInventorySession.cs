using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class BookInventorySession
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string StartedByUserId { get; set; } = string.Empty;

        public ApplicationUser? StartedBy { get; set; }

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAtUtc { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public ICollection<BookInventoryScan> Scans { get; set; } = new List<BookInventoryScan>();
    }
}
