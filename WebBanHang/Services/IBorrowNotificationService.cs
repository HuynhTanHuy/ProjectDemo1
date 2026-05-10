using WebBanHang.Models;

namespace WebBanHang.Services
{
    public interface IBorrowNotificationService
    {
        Task SendDueDateReminderAsync(Borrow borrow, int daysUntilDue, CancellationToken cancellationToken = default);

        Task SendOverdueReminderAsync(Borrow borrow, CancellationToken cancellationToken = default);
    }
}
