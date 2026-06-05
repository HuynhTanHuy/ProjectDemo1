using Microsoft.AspNetCore.Mvc.Rendering;
using WebBanHang.Models;

namespace WebBanHang.Models.ViewModels
{
    public class BookCopyIndexViewModel
    {
        public List<BookCopyListItemViewModel> Items { get; set; } = new();

        public string? SearchQuery { get; set; }

        /// <summary>Lọc trạng thái vật lý: Active, Lost, Disposed.</summary>
        public BookCopyStatus? PhysicalStatus { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public int TotalCount { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize));

        public List<SelectListItem> PhysicalStatusOptions { get; set; } = new();
    }
}
