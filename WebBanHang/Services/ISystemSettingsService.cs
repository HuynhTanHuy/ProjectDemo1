using WebBanHang.Models;

namespace WebBanHang.Services
{
    public interface ISystemSettingsService
    {
        Task<SystemSetting> GetAsync(CancellationToken cancellationToken = default);

        void InvalidateCache();
    }
}
