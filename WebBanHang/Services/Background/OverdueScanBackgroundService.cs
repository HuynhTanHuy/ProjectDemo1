namespace WebBanHang.Services.Background
{
    /// <summary>
    /// Chạy định kỳ (mặc định 6 giờ) để cập nhật quá hạn — đủ đảm bảo xử lý hằng ngày mà không cần thêm framework.
    /// </summary>
    public class OverdueScanBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OverdueScanBackgroundService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(6);

        public OverdueScanBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<OverdueScanBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OverdueScanBackgroundService started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IOverdueProcessingService>();
                    await svc.RunDailyOverdueAndRemindersAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Lỗi khi chạy job quá hạn mượn sách.");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}
