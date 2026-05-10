namespace WebBanHang.Models.ViewModels
{
    public class InventoryScanResultViewModel
    {
        public int SessionId { get; set; }

        public int BookCopyId { get; set; }

        public string CopyCode { get; set; } = string.Empty;

        public string BookTitle { get; set; } = string.Empty;

        public bool WrongShelf { get; set; }

        public string? Message { get; set; }
    }
}
