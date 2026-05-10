namespace WebBanHang.Models.ViewModels
{
    public class BorrowHistoryPageViewModel
    {
        public PagedResult<CustomerBorrowRowViewModel> Page { get; set; } = new();
    }
}
