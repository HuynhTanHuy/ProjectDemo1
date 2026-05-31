namespace WebBanHang.Models.DTOs
{
    public enum NotificationType
    {
        OverdueBorrow = 1,
        NewPenalty = 2,
        NewMember = 3,
        LowStock = 4,
        OutOfStock = 5,
        NewBorrow = 6,
        NewReturn = 7,
        LockedAccount = 8,
        SystemReport = 9,
        AdminOther = 10
    }

    public class NotificationItemDto
    {
        public NotificationType Type { get; set; }

        public string IconClass { get; set; } = "bi bi-bell";

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public int Count { get; set; }

        public DateTime? OccurredAt { get; set; }

        public string? Url { get; set; }
    }

    public class AdminNotificationsDto
    {
        public int TotalUnreadCount { get; set; }

        public List<NotificationItemDto> Items { get; set; } = new();
    }
}
