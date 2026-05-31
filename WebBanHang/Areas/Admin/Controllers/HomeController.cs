using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;
using WebBanHang.Services;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBorrowStatisticsService _borrowStats;

        public HomeController(ApplicationDbContext db, IBorrowStatisticsService borrowStats)
        {
            _db = db;
            _borrowStats = borrowStats;
        }

        public async Task<IActionResult> Index()
        {
            var totalBooks = await _db.Products.CountAsync();
            var availableBooks = await _db.Products.CountAsync(p => p.Stock > 0);
            var borrowedBooks = await _borrowStats.GetCurrentBorrowingCountAsync();
            var overdueBooks = await _borrowStats.GetOverdueCountAsync();
            var totalUsers = await _db.Users.CountAsync();
            var totalPenalties = await _db.Penalties.CountAsync();
            var unpaidPenalties = await _db.Penalties.CountAsync(x => !x.IsPaid);
            var unpaidAmount = await _db.Penalties.Where(x => !x.IsPaid).SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var availabilityPercent = totalBooks > 0
                ? (int)Math.Round(100.0 * availableBooks / totalBooks)
                : 0;
            var borrowedPercent = totalBooks > 0
                ? (int)Math.Min(100, Math.Round(100.0 * borrowedBooks / Math.Max(totalBooks, 1)))
                : 0;

            var activeBorrows = await _db.Borrows
                .AsNoTracking()
                .Where(b => b.Status == BorrowStatus.Borrowing || b.Status == BorrowStatus.Overdue)
                .Include(b => b.Book)
                .ThenInclude(p => p!.Category)
                .ToListAsync();

            var byCategory = activeBorrows
                .GroupBy(b => b.Book?.Category?.Name ?? "Không phân loại")
                .OrderByDescending(g => g.Count())
                .Take(8)
                .ToList();

            var categoryLabels = byCategory.Select(g => g.Key).ToList();
            var categoryValues = byCategory.Select(g => g.Count()).ToList();

            var now = DateTime.UtcNow;
            var trendLabels = new List<string>();
            var trendValues = new List<int>();
            for (var i = 5; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                trendLabels.Add(monthStart.ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture));
                var count = await _db.Borrows.CountAsync(b =>
                    b.BorrowDate >= monthStart && b.BorrowDate < monthEnd);
                trendValues.Add(count);
            }

            ViewData["Title"] = "Tổng quan";
            ViewData["AdminNavSection"] = "overview";
            ViewData["AdminPageTitle"] = "Tổng quan";
            ViewData["AdminBreadcrumb"] = "Tổng quan";

            var model = new DashboardViewModel
            {
                TotalBooks = totalBooks,
                AvailableBooks = availableBooks,
                BorrowedBooks = borrowedBooks,
                OverdueBooks = overdueBooks,
                TotalUsers = totalUsers,
                TotalPenalties = totalPenalties,
                UnpaidPenalties = unpaidPenalties,
                UnpaidPenaltyAmount = unpaidAmount,
                AvailabilityPercent = availabilityPercent,
                BorrowedPercent = borrowedPercent,
                BorrowByCategoryLabels = categoryLabels,
                BorrowByCategoryValues = categoryValues,
                BorrowTrendMonthLabels = trendLabels,
                BorrowTrendValues = trendValues
            };

            return View(model);
        }
    }
}
