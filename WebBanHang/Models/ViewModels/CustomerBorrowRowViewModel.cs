using WebBanHang.Models;

namespace WebBanHang.Models.ViewModels
{
    public class CustomerBorrowRowViewModel
    {
        public int BorrowId { get; set; }
        public int BookId { get; set; }
        public int? BookCopyId { get; set; }
        public string? CopyCode { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDateUtc { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime? ReturnDateUtc { get; set; }
        public BorrowStatus Status { get; set; }
        public int? DaysRemaining { get; set; }
        public decimal BorrowFeeAmount { get; set; }
        public decimal FineAmount { get; set; }
        public int OverdueDays { get; set; }
    }
}
