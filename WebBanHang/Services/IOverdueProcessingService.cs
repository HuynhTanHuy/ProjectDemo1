namespace WebBanHang.Services
{
    public interface IOverdueProcessingService
    {
        /// <summary>Quét quá hạn, cập nhật phạt, gửi nhắc (idempotent theo ngày).</summary>
        Task RunDailyOverdueAndRemindersAsync(CancellationToken cancellationToken = default);
    }
}
