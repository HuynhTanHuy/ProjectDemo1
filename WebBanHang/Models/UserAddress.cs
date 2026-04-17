using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class UserAddress
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required, StringLength(160)]
        public string FullName { get; set; }

        [Required, StringLength(200)]
        public string Street { get; set; }

        [Required, StringLength(120)]
        public string City { get; set; }

        [Required, StringLength(120)]
        public string State { get; set; }

        [Required, StringLength(30)]
        public string PostalCode { get; set; }

        [Required, StringLength(120)]
        public string Country { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        public bool IsDefault { get; set; }
    }
}



