using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public enum BorrowStatus
    {
        Borrowing = 1,
        Returned = 2
    }

    public class Borrow
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int BookId { get; set; }

        public DateTime BorrowDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        public BorrowStatus Status { get; set; } = BorrowStatus.Borrowing;

        public ApplicationUser? User { get; set; }
        public Product? Book { get; set; }
    }
}
