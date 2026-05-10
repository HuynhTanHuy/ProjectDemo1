using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models.ViewModels
{
    public class CheckoutOrderViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        public decimal MerchandiseTotal { get; set; }

        public decimal ShippingCost { get; set; }

        public decimal OrderTotal => MerchandiseTotal + ShippingCost;
    }
}
