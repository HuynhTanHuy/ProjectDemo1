using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class Author
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; }

        [StringLength(2000)]
        public string? Biography { get; set; }

        [StringLength(200)]
        public string? PhotoUrl { get; set; }

        public List<Product>? Products { get; set; }
    }
}



