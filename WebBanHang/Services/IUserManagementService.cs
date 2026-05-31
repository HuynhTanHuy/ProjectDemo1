using WebBanHang.Models.ViewModels;

namespace WebBanHang.Services
{
    public interface IUserManagementService
    {
        Task<UserIndexViewModel> GetUsersAsync(UserIndexViewModel filter);

        Task<UserDetailViewModel?> GetUserDetailAsync(string id);

        Task<(bool Success, string? Error)> CreateUserAsync(UserCreateViewModel model);

        Task<(bool Success, string? Error)> UpdateUserAsync(UserEditViewModel model);

        Task<(bool Success, string? Error)> ChangePasswordAsync(string userId, string newPassword);

        Task<(bool Success, string? Error)> LockUserAsync(string userId, string currentUserId);

        Task<(bool Success, string? Error)> UnlockUserAsync(string userId);

        Task<(bool Success, string? Error)> DeleteUserAsync(string userId, string currentUserId);

        Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetRoleSelectListAsync(string? selectedRole = null);
    }
}
