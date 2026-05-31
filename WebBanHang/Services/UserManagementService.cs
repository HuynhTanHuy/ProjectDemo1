using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;

namespace WebBanHang.Services
{
    public class UserManagementService : IUserManagementService
    {
        private static readonly string[] AssignableRoles =
        {
            SD.Role_Admin,
            SD.Role_User,
            SD.Role_Customer,
            SD.Role_Employee,
            SD.Role_Company
        };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public async Task<UserIndexViewModel> GetUsersAsync(UserIndexViewModel filter)
        {
            if (filter.PageNumber < 1) filter.PageNumber = 1;
            if (filter.PageSize < 1) filter.PageSize = 10;

            filter.RoleOptions = await GetRoleSelectListAsync(filter.RoleFilter);
            filter.RoleOptions.Insert(0, new SelectListItem { Value = "", Text = "Tất cả vai trò" });

            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var s = filter.SearchQuery.Trim();
                query = query.Where(u =>
                    (u.UserName != null && u.UserName.Contains(s)) ||
                    (u.Email != null && u.Email.Contains(s)) ||
                    u.FullName.Contains(s));
            }

            var users = await query
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var filtered = new List<ApplicationUser>();
            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(filter.RoleFilter))
                {
                    filtered.Add(user);
                    continue;
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(filter.RoleFilter))
                {
                    filtered.Add(user);
                }
            }

            filter.TotalCount = filtered.Count;
            var pageUsers = filtered
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            filter.Items = new List<UserListItemViewModel>();
            foreach (var user in pageUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                filter.Items.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    FullName = user.FullName,
                    Roles = roles.ToList(),
                    IsLockedOut = await _userManager.IsLockedOutAsync(user),
                    EmailConfirmed = user.EmailConfirmed
                });
            }

            return filter;
        }

        public async Task<UserDetailViewModel?> GetUserDetailAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var activeBorrows = await _db.Borrows.CountAsync(b =>
                b.UserId == id &&
                (b.Status == BorrowStatus.Borrowing || b.Status == BorrowStatus.Overdue));
            var totalOrders = await _db.Orders.CountAsync(o => o.UserId == id);

            return new UserDetailViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                FullName = user.FullName,
                Address = user.Address,
                Age = user.Age,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                IsLockedOut = await _userManager.IsLockedOutAsync(user),
                Roles = roles.ToList(),
                ActiveBorrows = activeBorrows,
                TotalOrders = totalOrders
            };
        }

        public async Task<(bool Success, string? Error)> CreateUserAsync(UserCreateViewModel model)
        {
            if (!AssignableRoles.Contains(model.Role))
            {
                return (false, "Vai trò không hợp lệ.");
            }

            await EnsureRoleExistsAsync(model.Role);

            var user = new ApplicationUser
            {
                UserName = model.UserName.Trim(),
                Email = model.Email.Trim(),
                FullName = model.FullName.Trim(),
                Address = model.Address,
                Age = model.Age,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            await _userManager.AddToRoleAsync(user, model.Role);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateUserAsync(UserEditViewModel model)
        {
            if (!AssignableRoles.Contains(model.Role))
            {
                return (false, "Vai trò không hợp lệ.");
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return (false, "Không tìm thấy tài khoản.");

            user.UserName = model.UserName.Trim();
            user.Email = model.Email.Trim();
            user.FullName = model.FullName.Trim();
            user.Address = model.Address;
            user.Age = model.Age;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            await EnsureRoleExistsAsync(model.Role);
            await _userManager.AddToRoleAsync(user, model.Role);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> ChangePasswordAsync(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "Không tìm thấy tài khoản.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> LockUserAsync(string userId, string currentUserId)
        {
            if (userId == currentUserId)
            {
                return (false, "Không thể khóa tài khoản của chính bạn.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "Không tìm thấy tài khoản.");

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UnlockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "Không tìm thấy tài khoản.");

            await _userManager.SetLockoutEndDateAsync(user, null);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteUserAsync(string userId, string currentUserId)
        {
            if (userId == currentUserId)
            {
                return (false, "Không thể xóa tài khoản của chính bạn.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "Không tìm thấy tài khoản.");

            var hasActiveBorrows = await _db.Borrows.AnyAsync(b =>
                b.UserId == userId &&
                (b.Status == BorrowStatus.Borrowing || b.Status == BorrowStatus.Overdue));
            if (hasActiveBorrows)
            {
                return (false, "Không thể xóa tài khoản còn phiếu mượn đang hoạt động.");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            return (true, null);
        }

        public async Task<List<SelectListItem>> GetRoleSelectListAsync(string? selectedRole = null)
        {
            foreach (var role in AssignableRoles)
            {
                await EnsureRoleExistsAsync(role);
            }

            return AssignableRoles
                .Select(r => new SelectListItem
                {
                    Value = r,
                    Text = r,
                    Selected = r == selectedRole
                })
                .ToList();
        }

        private async Task EnsureRoleExistsAsync(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
