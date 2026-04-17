using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class Genre
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; }

        public List<Product>? Products { get; set; }
    }
}



