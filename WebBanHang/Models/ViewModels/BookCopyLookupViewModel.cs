using WebBanHang.Models;

namespace WebBanHang.Models.ViewModels
{
    public class BookCopyLookupViewModel
    {
        public int BookCopyId { get; set; }

        public string CopyCode { get; set; } = string.Empty;

        /// <summary>Đường dẫn tương đối ảnh QR (wwwroot).</summary>
        public string? QrImageRelativeUrl { get; set; }

        public int BookId { get; set; }

        public string BookTitle { get; set; } = string.Empty;

        public string? AuthorName { get; set; }

        public string CopyStatus { get; set; } = string.Empty;

        public string? ShelfLocation { get; set; }

        public string? BorrowedByUserName { get; set; }

        public string? BorrowedByFullName { get; set; }

        public DateTime? DueDateUtc { get; set; }

        public BorrowStatus? ActiveBorrowStatus { get; set; }
    }
}
