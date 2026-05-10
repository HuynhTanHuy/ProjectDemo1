using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; }
        public string? Address { get; set; }
        public string? Age { get; set; }

        /// <summary>Mã thẻ thư viện (opaque) — nội dung QR thành viên, không chứa PII.</summary>
        [MaxLength(64)]
        public string? LibraryMemberQrToken { get; set; }

        [MaxLength(512)]
        public string? LibraryMemberQrImageRelativePath { get; set; }
    }
}
