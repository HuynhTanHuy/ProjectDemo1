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
        public int StatBorrowing { get; set; }
        public int StatAvailableTitles { get; set; }
        public int StatOverdueLoans { get; set; }

        public int TotalFilteredCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
