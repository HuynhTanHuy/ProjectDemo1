using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBanHang.Models;
using WebBanHang.Models.ViewModels;

namespace WebBanHang.Controllers
{
    public class AuthController : Controller
    {
        private static readonly string[] AllowedRegisterRoles =
        {
            SD.Role_Customer,
            SD.Role_User
        };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!model.AcceptTerms)
            {
                ModelState.AddModelError(nameof(model.AcceptTerms), "You must accept the terms and conditions.");
            }

            if (!AllowedRegisterRoles.Contains(model.Role, StringComparer.Ordinal))
            {
                ModelState.AddModelError(nameof(model.Role), "Invalid role selected.");
                model.Role = SD.Role_Customer;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Error = FormatModelErrors();
                return View(model);
            }

            var username = model.Username.Trim();
            var email = model.Email.Trim();
            var firstName = model.FirstName.Trim();
            var lastName = model.LastName.Trim();
            var fullName = $"{firstName} {lastName}".Trim();

            if (await _userManager.FindByNameAsync(username) != null)
            {
                ViewBag.Error = "This username is already taken.";
                model.Username = username;
                model.Email = email;
                model.FirstName = firstName;
                model.LastName = lastName;
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(email) != null)
            {
                ViewBag.Error = "This email is already registered.";
                model.Username = username;
                model.Email = email;
                model.FirstName = firstName;
                model.LastName = lastName;
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = fullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                ViewBag.Error = string.Join(" ", result.Errors.Select(e => e.Description));
                model.Username = username;
                model.Email = email;
                model.FirstName = firstName;
                model.LastName = lastName;
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var rememberMe = string.Equals(Request.Form["rememberMe"], "true", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Vui lòng nhập email hoặc tên đăng nhập.";
                ViewBag.Email = email ?? string.Empty;
                return View();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập mật khẩu.";
                ViewBag.Email = email.Trim();
                return View();
            }

            var userNameOrEmail = email.Trim();
            ApplicationUser? user;
            if (userNameOrEmail.Contains('@'))
            {
                user = await _userManager.FindByEmailAsync(userNameOrEmail);
            }
            else
            {
                user = await _userManager.FindByNameAsync(userNameOrEmail);
            }

            if (user == null)
            {
                ViewBag.Error = "Email hoặc mật khẩu không đúng.";
                ViewBag.Email = userNameOrEmail;
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, password, rememberMe, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                ViewBag.Error = "Tài khoản đã bị khóa. Vui lòng thử lại sau.";
                ViewBag.Email = userNameOrEmail;
                return View();
            }

            if (!result.Succeeded)
            {
                ViewBag.Error = "Email hoặc mật khẩu không đúng.";
                ViewBag.Email = userNameOrEmail;
                return View();
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home", new { area = "Customer" });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        private string FormatModelErrors()
        {
            return string.Join(
                " ",
                ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid input." : e.ErrorMessage));
        }
    }
}
