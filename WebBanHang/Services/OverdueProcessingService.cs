using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class OverdueProcessingService : IOverdueProcessingService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISystemSettingsService _settings;
        private readonly IBorrowNotificationService _notifications;
        private readonly ILogger<OverdueProcessingService> _logger;

        public OverdueProcessingService(
            ApplicationDbContext db,
            ISystemSettingsService settings,
            IBorrowNotificationService notifications,
            ILogger<OverdueProcessingService> logger)
        {
            _db = db;
            _settings = settings;
            _notifications = notifications;
            _logger = logger;
        }

        public async Task RunDailyOverdueAndRemindersAsync(CancellationToken cancellationToken = default)
        {
            var cfg = await _settings.GetAsync(cancellationToken);
            var utcNow = DateTime.UtcNow;
            var today = utcNow.Date;

            var active = await _db.Borrows
                .Where(x => x.Status == BorrowStatus.Borrowing || x.Status == BorrowStatus.Overdue)
                .ToListAsync(cancellationToken);

            foreach (var borrow in active)
            {
                if (borrow.DueDate.Date < today)
                {
                    if (borrow.Status != BorrowStatus.Overdue)
                    {
                        borrow.Status = BorrowStatus.Overdue;
                        _logger.LogInformation("Borrow {Id} chuyển trạng thái Overdue.", borrow.Id);
                    }

                    var overdueDays = (today - borrow.DueDate.Date).Days;
                    borrow.OverdueDays = overdueDays;
                    borrow.FineAmount = overdueDays * cfg.OverdueFeePerDay;
                }
                else
                {
                    borrow.OverdueDays = 0;
                    borrow.FineAmount = 0;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            if (cfg.RemindBeforeDueDays > 0)
            {
                var dueSoon = await _db.Borrows
                    .Where(x => x.Status == BorrowStatus.Borrowing)
                    .ToListAsync(cancellationToken);

                foreach (var b in dueSoon)
                {
                    var daysLeft = (b.DueDate.Date - today).Days;
                    if (daysLeft >= 0 && daysLeft <= cfg.RemindBeforeDueDays)
                    {
                        var shouldSend = b.LastDueReminderSentAtUtc == null
                                         || b.LastDueReminderSentAtUtc.Value.Date < today;
                        if (shouldSend)
                        {
                            await _notifications.SendDueDateReminderAsync(b, daysLeft, cancellationToken);
                            b.LastDueReminderSentAtUtc = utcNow;
                        }
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
            }

            var overdueList = await _db.Borrows
                .Where(x => x.Status == BorrowStatus.Overdue)
                .ToListAsync(cancellationToken);

            foreach (var b in overdueList)
            {
                var shouldSend = b.LastOverdueReminderSentAtUtc == null
                                 || b.LastOverdueReminderSentAtUtc.Value.Date < today;
                if (shouldSend)
                {
                    await _notifications.SendOverdueReminderAsync(b, cancellationToken);
                    b.LastOverdueReminderSentAtUtc = utcNow;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
