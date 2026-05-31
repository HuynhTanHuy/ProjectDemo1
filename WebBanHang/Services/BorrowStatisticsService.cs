using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class BorrowStatisticsService : IBorrowStatisticsService
    {
        private readonly ApplicationDbContext _db;

        public BorrowStatisticsService(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<int> GetCurrentBorrowingCountAsync()
        {
            return _db.Borrows.CountAsync(b =>
                b.Status == BorrowStatus.Borrowing || b.Status == BorrowStatus.Overdue);
        }

        public Task<int> GetOverdueCountAsync()
        {
            var utcToday = DateTime.UtcNow.Date;
            return _db.Borrows.CountAsync(b =>
                b.Status == BorrowStatus.Overdue ||
                (b.Status == BorrowStatus.Borrowing && b.DueDate.Date < utcToday));
        }

        public Task<int> GetReturnedCountAsync()
        {
            return _db.Borrows.CountAsync(b => b.Status == BorrowStatus.Returned);
        }

        public Task<int> GetTotalBorrowCountAsync()
        {
            return _db.Borrows.CountAsync();
        }
    }
}
