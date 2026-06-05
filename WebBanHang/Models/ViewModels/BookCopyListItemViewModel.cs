using WebBanHang.Models;

namespace WebBanHang.Models.ViewModels
{
    public class BookCopyListItemViewModel
    {
        public int BookCopyId { get; set; }

        public string CopyCode { get; set; } = string.Empty;

        public string BookTitle { get; set; } = string.Empty;

        public string? QrImageRelativeUrl { get; set; }

        public string? ShelfLocation { get; set; }

        public BookCopyStatus PhysicalStatus { get; set; }

        /// <summary>Trạng thái mượn suy ra từ Borrow active.</summary>
        public string BorrowStatusText { get; set; } = string.Empty;

        public string? BorrowedByUserName { get; set; }

        public string? BorrowedByFullName { get; set; }
    }
}
