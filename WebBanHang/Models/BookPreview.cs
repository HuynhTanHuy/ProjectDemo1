using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public enum PreviewType
    {
        Pdf = 1,
        Text = 2
    }

    public class BookPreview
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public int BookId { get; set; }

        public Product? Book { get; set; }

        [Required]
        public PreviewType PreviewType { get; set; }

        [StringLength(500)]
        public string? FilePath { get; set; }

        public string? Content { get; set; }

        [Range(1, int.MaxValue)]
        public int TotalPages { get; set; }

        [Range(1, int.MaxValue)]
        public int PreviewPages { get; set; }

        public bool AllowDownload { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
