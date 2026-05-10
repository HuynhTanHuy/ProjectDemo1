using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class BookInventoryScan
    {
        public int Id { get; set; }

        public int SessionId { get; set; }

        public BookInventorySession? Session { get; set; }

        public int BookCopyId { get; set; }

        public BookCopy? BookCopy { get; set; }

        public DateTime ScannedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Vị trí ghi nhận khi quét (để so với ShelfLocation trên bản sao).</summary>
        [MaxLength(120)]
        public string? ObservedShelfLocation { get; set; }
    }
}
