using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; }
        [Range(0.01, 10000.00)]
        public decimal Price { get; set; }
        [Required]
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public List<ProductImage>? Images { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // Book-specific fields
        [StringLength(20)]
        public string? Isbn { get; set; }

        public int? AuthorId { get; set; }
        public Author? Author { get; set; }

        public int? PublisherId { get; set; }
        public Publisher? Publisher { get; set; }

        public int? GenreId { get; set; }
        public Genre? Genre { get; set; }

        public DateTime? PublicationDate { get; set; }

        public int? PageCount { get; set; }

        [StringLength(40)]
        public string? Language { get; set; }

        public int Stock { get; set; } = 0;

        [Range(0, 100)]
        public int? DiscountPercent { get; set; }

        [StringLength(160)]
        public string? Slug { get; set; }

        public List<Review>? Reviews { get; set; }
    }
}
