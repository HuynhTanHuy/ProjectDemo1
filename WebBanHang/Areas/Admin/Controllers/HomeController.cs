using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var totalBooks = await _db.Products.CountAsync();
            var availableBooks = await _db.Products.CountAsync(p => p.Stock > 0);
            var borrowedBooks = await _db.Borrows.CountAsync(x => x.Status == BorrowStatus.Borrowing);
            var overdueBooks = await _db.Borrows.CountAsync(x =>
                x.Status == BorrowStatus.Borrowing && x.DueDate < DateTime.UtcNow);
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
                .Where(b => b.Status == BorrowStatus.Borrowing)
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

