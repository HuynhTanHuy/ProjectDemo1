using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class Publisher
    {
        public int Id { get; set; }

        [Required, StringLength(160)]
        public string Name { get; set; }

        [StringLength(200)]
        public string? LogoUrl { get; set; }

        [StringLength(300)]
        public string? Website { get; set; }

        public List<Product>? Products { get; set; }
    }
}



