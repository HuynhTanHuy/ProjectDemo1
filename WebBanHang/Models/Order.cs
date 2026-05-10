using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebBanHang.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;

        [ForeignKey("UserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; } = null!;

        [ValidateNever]
        public List<OrderDetail> OrderDetails { get; set; } = new();

        [ValidateNever]
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
