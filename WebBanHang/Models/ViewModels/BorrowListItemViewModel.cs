using WebBanHang.Models;

namespace WebBanHang.Models.ViewModels
{
    public class BorrowListItemViewModel
    {
        public int BorrowId { get; set; }
        public int? BookCopyId { get; set; }
        public string? CopyCode { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public BorrowStatus Status { get; set; }
        public bool IsOverdue { get; set; }
        public decimal FineAmount { get; set; }
        public int OverdueDays { get; set; }
    }
}
