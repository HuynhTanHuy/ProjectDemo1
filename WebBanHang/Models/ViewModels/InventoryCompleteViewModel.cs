namespace WebBanHang.Models.ViewModels
{
    public class InventoryCompleteViewModel
    {
        public int SessionId { get; set; }

        public int TotalCopiesInLibrary { get; set; }

        public int ScannedCount { get; set; }

        public IReadOnlyList<string> MissingCopyCodes { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> WrongShelfLines { get; set; } = Array.Empty<string>();
    }
}
