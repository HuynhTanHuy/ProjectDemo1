using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebBanHang.Models.ViewModels
{
    public class ProductBooksPageViewModel
    {
        public List<BookViewModel> Books { get; set; } = new();

        public int? GenreId { get; set; }

        /// <summary>Lọc: (rỗng), available, borrowing, overdue, out</summary>
        public string? Status { get; set; }

        public List<SelectListItem> GenreOptions { get; set; } = new();

        public int StatTotalBooks { get; set; }

        /// <summary>Phiếu còn hoạt động (Borrowing + Overdue), toàn hệ thống — cùng định nghĩa với BorrowService.</summary>
        public int StatActiveLoansSystemWide { get; set; }

        /// <summary>Phiếu hoạt động của user hiện tại; 0 khi ẩn danh.</summary>
        public int StatMyActiveLoans { get; set; }

        /// <summary>Tương thích Admin/catalog: bằng <see cref="StatActiveLoansSystemWide"/>.</summary>
        public int StatBorrowing { get; set; }

        public int StatAvailableTitles { get; set; }

        /// <summary>Phiếu hoạt động đã quá hạn ngày trả (DueDate &lt; hôm nay).</summary>
        public int StatOverdueLoans { get; set; }

        public int TotalFilteredCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
