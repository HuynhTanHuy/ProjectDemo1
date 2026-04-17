using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ✅ Kết nối CSDL SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Cấu hình Identity (quản lý tài khoản, quyền hạn)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// ✅ Cấu hình Cookie Authentication (đăng nhập, đăng xuất, từ chối quyền truy cập)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";        // Trang đăng nhập
    options.LogoutPath = "/Identity/Account/Logout";      // Trang đăng xuất
    options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // Trang báo lỗi khi truy cập trái phép
});

// ✅ Thêm dịch vụ Session (Lưu trữ dữ liệu tạm thời)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Timeout sau 30 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ Thêm dịch vụ Razor Pages & Controllers
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ✅ Đăng ký Repository (Dependency Injection)
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();

builder.Services.AddMvc().AddRazorPagesOptions(options => {
    options.Conventions.AddAreaPageRoute("Identity", "/Account/Login", "");
});

var app = builder.Build();

// ✅ Cấu hình Middleware trong Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession(); // 🔹 Bổ sung Session vào Middleware
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ✅ Cấu hình Endpoint cho Areas & Controllers
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "Admin",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    endpoints.MapControllerRoute(
        name: "Customer",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    endpoints.MapRazorPages();
});

app.Run();
