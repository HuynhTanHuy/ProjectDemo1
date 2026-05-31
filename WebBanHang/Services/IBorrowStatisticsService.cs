namespace WebBanHang.Services
{
    public interface IBorrowStatisticsService
    {
        /// <summary>Phiếu mượn đang hoạt động (Borrowing hoặc Overdue).</summary>
        Task<int> GetCurrentBorrowingCountAsync();

        /// <summary>Phiếu quá hạn (Overdue hoặc Borrowing quá DueDate).</summary>
        Task<int> GetOverdueCountAsync();

        Task<int> GetReturnedCountAsync();

        Task<int> GetTotalBorrowCountAsync();
    }
}
