using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

        public SystemSettingsService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<SystemSetting> GetAsync(CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetOrCreateAsync(
                "system_settings_singleton",
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                    var row = await _db.SystemSettings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == SystemSetting.SingletonId, cancellationToken);

                    if (row == null)
                    {
                        throw new InvalidOperationException(
                            "Chưa có cấu hình SystemSettings (Id=1). Chạy migration và cập nhật CSDL.");
                    }

                    return row;
                });

            return cached ?? throw new InvalidOperationException("Không đọc được SystemSettings.");
        }

        public void InvalidateCache()
        {
            _cache.Remove("system_settings_singleton");
        }
    }
}
