namespace WebBanHang.Models.ViewModels
{
    public class BookViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Author { get; set; } = "—";
        public string Category { get; set; } = "—";
        public string Genre { get; set; } = "—";
        public string? Isbn { get; set; }
        public int Quantity { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusKey { get; set; } = string.Empty;

        public double AvgRating { get; set; }
        public int? PublicationYear { get; set; }
        public string? Language { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
