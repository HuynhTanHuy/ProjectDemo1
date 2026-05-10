using WebBanHang.Models;

namespace WebBanHang.Services
{
    /// <summary>
    /// Mặc định: ghi log (production có thể thay bằng SMTP / queue).
    /// </summary>
    public class LoggingBorrowNotificationService : IBorrowNotificationService
    {
        private readonly ILogger<LoggingBorrowNotificationService> _logger;

        public LoggingBorrowNotificationService(ILogger<LoggingBorrowNotificationService> logger)
        {
            _logger = logger;
        }

        public Task SendDueDateReminderAsync(Borrow borrow, int daysUntilDue, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[BorrowReminder] User {UserId} borrow {BorrowId} sách {BookId}: còn {Days} ngày đến hạn {Due:d}.",
                borrow.UserId,
                borrow.Id,
                borrow.BookId,
                daysUntilDue,
                borrow.DueDate);
            return Task.CompletedTask;
        }

        public Task SendOverdueReminderAsync(Borrow borrow, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning(
                "[BorrowOverdue] User {UserId} borrow {BorrowId} sách {BookId}: quá hạn {OverdueDays} ngày, phạt hiện tại {Fine}.",
                borrow.UserId,
                borrow.Id,
                borrow.BookId,
                borrow.OverdueDays,
                borrow.FineAmount);
            return Task.CompletedTask;
        }
    }
}
