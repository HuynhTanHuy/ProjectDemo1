using WebBanHang.Models;

namespace WebBanHang.Models.ViewModels
{
    public class CustomerHomeIndexViewModel
    {
        public string Tab { get; set; } = "products";

        public ProductBooksPageViewModel? Catalog { get; set; }

        public List<Author>? Authors { get; set; }
        public List<Genre>? Genres { get; set; }
        public List<Publisher>? Publishers { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 12;

        public string? Query { get; set; }
        public int? CategoryId { get; set; }
        public int? AuthorId { get; set; }
        public int? GenreId { get; set; }
        public int? PublisherId { get; set; }
        public string? Status { get; set; }

        public string Sort { get; set; } = "popular";
        public int? MinRating { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public string? Lang { get; set; }
    }
}
