using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.DTOs;

namespace WebBanHang.Services
{
    public class NotificationService : INotificationService
    {
        private const int LowStockThreshold = 3;
        private const int RecentDays = 7;

        private readonly ApplicationDbContext _db;
        private readonly IBorrowStatisticsService _borrowStats;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(
            ApplicationDbContext db,
            IBorrowStatisticsService borrowStats,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _borrowStats = borrowStats;
            _userManager = userManager;
        }

        public async Task<AdminNotificationsDto> GetAdminNotificationsAsync(int maxItems = 12)
        {
            var utcNow = DateTime.UtcNow;
            var since = utcNow.AddDays(-RecentDays);
            var items = new List<NotificationItemDto>();

            var overdueCount = await _borrowStats.GetOverdueCountAsync();
            if (overdueCount > 0)
            {
                items.Add(Make(
                    NotificationType.OverdueBorrow,
                    "bi bi-exclamation-triangle-fill text-warning",
                    $"{overdueCount} phiếu mượn quá hạn",
                    "Cần xử lý trả sách hoặc liên hệ thành viên",
                    overdueCount,
                    "/Admin/OverdueReport"));
            }

            var newPenaltyCount = await _db.Penalties.CountAsync(p =>
                !p.IsPaid && p.CreatedAt >= since);
            if (newPenaltyCount > 0)
            {
                items.Add(Make(
                    NotificationType.NewPenalty,
                    "bi bi-cash-coin text-danger",
                    $"{newPenaltyCount} phiếu phạt mới phát sinh",
                    $"Trong {RecentDays} ngày gần nhất, chưa thanh toán",
                    newPenaltyCount,
                    "/Admin/Penalties"));
            }

            var newMemberCount = await CountFirstTimeBorrowersAsync(since);
            if (newMemberCount > 0)
            {
                items.Add(Make(
                    NotificationType.NewMember,
                    "bi bi-person-plus-fill text-success",
                    $"{newMemberCount} thành viên mới",
                    "Lần đầu mượn sách trong tuần qua",
                    newMemberCount,
                    "/Admin/Users"));
            }

            var lowStockCount = await _db.Products.CountAsync(p =>
                p.Stock > 0 && p.Stock <= LowStockThreshold);
            if (lowStockCount > 0)
            {
                items.Add(Make(
                    NotificationType.LowStock,
                    "bi bi-box-seam text-warning",
                    $"{lowStockCount} đầu sách sắp hết",
                    $"Tồn kho ≤ {LowStockThreshold} bản",
                    lowStockCount,
                    "/Admin/Product"));
            }

            var outOfStockCount = await _db.Products.CountAsync(p => p.Stock <= 0);
            if (outOfStockCount > 0)
            {
                items.Add(Make(
                    NotificationType.OutOfStock,
                    "bi bi-book text-danger",
                    $"{outOfStockCount} đầu sách đã hết",
                    "Cần bổ sung tồn kho",
                    outOfStockCount,
                    "/Admin/Product?status=out"));
            }

            var newBorrowCount = await _db.Borrows.CountAsync(b => b.BorrowDate >= since);
            if (newBorrowCount > 0)
            {
                items.Add(Make(
                    NotificationType.NewBorrow,
                    "bi bi-journal-arrow-up text-primary",
                    $"{newBorrowCount} đơn mượn mới",
                    $"Trong {RecentDays} ngày gần nhất",
                    newBorrowCount,
                    "/Admin/Borrows?Status=Borrowing"));
            }

            var newReturnCount = await _db.Borrows.CountAsync(b =>
                b.Status == BorrowStatus.Returned &&
                b.ReturnDate != null &&
                b.ReturnDate >= since);
            if (newReturnCount > 0)
            {
                items.Add(Make(
                    NotificationType.NewReturn,
                    "bi bi-arrow-return-left text-success",
                    $"{newReturnCount} đơn trả mới",
                    $"Trong {RecentDays} ngày gần nhất",
                    newReturnCount,
                    "/Admin/Borrows?Status=Returned"));
            }

            var lockedCount = await CountLockedUsersAsync();
            if (lockedCount > 0)
            {
                items.Add(Make(
                    NotificationType.LockedAccount,
                    "bi bi-lock-fill text-secondary",
                    $"{lockedCount} tài khoản bị khóa",
                    "Kiểm tra và xử lý nếu cần",
                    lockedCount,
                    "/Admin/Users"));
            }

            var unpaidPenalties = await _db.Penalties.CountAsync(p => !p.IsPaid);
            var activeBorrows = await _borrowStats.GetCurrentBorrowingCountAsync();
            if (unpaidPenalties > 0 || overdueCount > 0)
            {
                items.Add(Make(
                    NotificationType.SystemReport,
                    "bi bi-clipboard-data text-info",
                    "Báo cáo hệ thống",
                    $"{activeBorrows} đang mượn · {unpaidPenalties} phạt chưa trả",
                    Math.Max(1, (overdueCount > 0 ? 1 : 0) + (unpaidPenalties > 0 ? 1 : 0)),
                    "/Admin/Home"));
            }

            var pendingOrders = await _db.Orders.CountAsync(o =>
                o.OrderStatus == OrderStatus.Pending || o.OrderStatus == OrderStatus.Paid);
            if (pendingOrders > 0)
            {
                items.Add(Make(
                    NotificationType.AdminOther,
                    "bi bi-bell-fill text-primary",
                    $"{pendingOrders} đơn hàng cần xử lý",
                    "Đơn chờ thanh toán hoặc đang xử lý",
                    pendingOrders,
                    "/Admin/Orders"));
            }

            var ordered = items
                .OrderByDescending(i => i.Count)
                .ThenBy(i => i.Type)
                .Take(maxItems)
                .ToList();

            return new AdminNotificationsDto
            {
                TotalUnreadCount = items.Sum(i => i.Count),
                Items = ordered
            };
        }

        private static NotificationItemDto Make(
            NotificationType type,
            string icon,
            string title,
            string message,
            int count,
            string url)
        {
            return new NotificationItemDto
            {
                Type = type,
                IconClass = icon,
                Title = title,
                Message = message,
                Count = count,
                OccurredAt = DateTime.UtcNow,
                Url = url
            };
        }

        private async Task<int> CountFirstTimeBorrowersAsync(DateTime since)
        {
            var recentBorrowers = await _db.Borrows
                .AsNoTracking()
                .Where(b => b.BorrowDate >= since)
                .Select(b => b.UserId)
                .Distinct()
                .ToListAsync();

            if (recentBorrowers.Count == 0) return 0;

            var count = 0;
            foreach (var userId in recentBorrowers)
            {
                var hadPrior = await _db.Borrows.AnyAsync(b =>
                    b.UserId == userId && b.BorrowDate < since);
                if (!hadPrior) count++;
            }

            return count;
        }

        private async Task<int> CountLockedUsersAsync()
        {
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            var count = 0;
            foreach (var user in users)
            {
                if (await _userManager.IsLockedOutAsync(user))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
