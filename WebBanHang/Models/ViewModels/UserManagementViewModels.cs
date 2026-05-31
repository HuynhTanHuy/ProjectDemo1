using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebBanHang.Models;

namespace WebBanHang.Models.ViewModels
{
    public class UserIndexViewModel
    {
        public List<UserListItemViewModel> Items { get; set; } = new();

        public string? SearchQuery { get; set; }

        public string? RoleFilter { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalCount { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize));

        public List<SelectListItem> RoleOptions { get; set; } = new();
    }

    public class UserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();

        public bool IsLockedOut { get; set; }

        public bool EmailConfirmed { get; set; }
    }

    public class UserDetailViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? Age { get; set; }

        public string? PhoneNumber { get; set; }

        public bool EmailConfirmed { get; set; }

        public bool LockoutEnabled { get; set; }

        public DateTimeOffset? LockoutEnd { get; set; }

        public bool IsLockedOut { get; set; }

        public List<string> Roles { get; set; } = new();

        public int ActiveBorrows { get; set; }

        public int TotalOrders { get; set; }
    }

    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(256)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? Age { get; set; }

        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        public string Role { get; set; } = SD.Role_Customer;

        public List<SelectListItem> RoleOptions { get; set; } = new();
    }

    public class UserEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? Age { get; set; }

        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        public string Role { get; set; } = string.Empty;

        public List<SelectListItem> RoleOptions { get; set; } = new();
    }

    public class UserChangePasswordViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
