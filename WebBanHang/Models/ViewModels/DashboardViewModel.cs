namespace WebBanHang.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int AvailableBooks { get; set; }
        public int BorrowedBooks { get; set; }
        public int OverdueBooks { get; set; }
        public int TotalUsers { get; set; }
        public int TotalPenalties { get; set; }
        public int UnpaidPenalties { get; set; }
        public decimal UnpaidPenaltyAmount { get; set; }

        /// <summary>0–100: ty le dau sach con ton kho (Stock lon hon 0).</summary>
        public int AvailabilityPercent { get; set; }

        /// <summary>0–100: tỷ lệ đang mượn so với tổng đầu sách.</summary>
        public int BorrowedPercent { get; set; }

        public List<string> BorrowByCategoryLabels { get; set; } = new();
        public List<int> BorrowByCategoryValues { get; set; } = new();

        public List<string> BorrowTrendMonthLabels { get; set; } = new();
        public List<int> BorrowTrendValues { get; set; } = new();
    }
}
