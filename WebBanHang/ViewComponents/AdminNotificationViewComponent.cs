using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.ViewComponents
{
    public class AdminNotificationViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;

        public AdminNotificationViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var utc = DateTime.UtcNow;
            var count = await _db.Borrows.CountAsync(b =>
                b.Status == BorrowStatus.Overdue ||
                (b.Status == BorrowStatus.Borrowing && b.DueDate.Date < utc.Date));
            return View(count);
        }
    }
}
