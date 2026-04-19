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
    options.LoginPath = "/Auth/Login";                      // Trang đăng nhập (MVC)
    options.LogoutPath = "/Identity/Account/Logout";
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { SD.Role_Admin, SD.Role_User, SD.Role_Customer };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

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
    // Tránh dùng lại Login Razor (RCL) khi có bookmark /Identity/Account/Login
    // Lưu ý: KHÔNG MapGet("/Customer") — route đó ăn cả GET /Customer?keyword=... (form search rút gọn URL),
    // redirect mất query string → keyword null. Area Customer + controller/action mặc định đã xử lý bởi MapControllerRoute.

    endpoints.MapGet("/Identity/Account/Login", context =>
    {
        var returnUrl = context.Request.Query["ReturnUrl"].FirstOrDefault()
            ?? context.Request.Query["returnUrl"].FirstOrDefault();
        var suffix = string.IsNullOrEmpty(returnUrl)
            ? string.Empty
            : "?returnUrl=" + Uri.EscapeDataString(returnUrl);
        context.Response.Redirect("/Auth/Login" + suffix);
        return Task.CompletedTask;
    });

    // URL thân thiện (tránh /Author bị hiểu nhầm là controller mặc định)
    endpoints.MapControllerRoute(
        name: "customerAuthorBrowser",
        pattern: "Author/{action=Index}/{id?}",
        defaults: new { area = "Customer", controller = "Authors" });

    endpoints.MapControllerRoute(
        name: "customerCategoryBrowser",
        pattern: "Category/{action=Index}/{id?}",
        defaults: new { area = "Customer", controller = "Genres" });

    endpoints.MapControllerRoute(
        name: "customerPublisherBrowser",
        pattern: "Publisher/{action=Index}/{id?}",
        defaults: new { area = "Customer", controller = "Publishers" });

    endpoints.MapControllers();

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
