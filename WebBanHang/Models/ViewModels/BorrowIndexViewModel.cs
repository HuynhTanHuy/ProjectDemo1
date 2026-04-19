using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebBanHang.Models.ViewModels
{
    /// <summary>
    /// Trang danh sách mượn sách (lọc, phân trang, thống kê).
    /// </summary>
    public class BorrowIndexViewModel
    {
        public List<BorrowListItemViewModel> Items { get; set; } = new();

        public string? SearchQuery { get; set; }

        public int? CategoryId { get; set; }

        /// <summary>
        /// Lọc: rỗng = tất cả; Returned; Borrowing; Overdue.
        /// </summary>
        public string? Status { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalCount { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize));

        // Thống kê (toàn hệ thống)
        public int StatTotalBorrows { get; set; }

        public int StatActiveBorrowing { get; set; }

        public int StatOverdue { get; set; }

        public int StatReturned { get; set; }

        public List<SelectListItem> CategoryOptions { get; set; } = new();
    }
}
