using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using DemoMVC.Models;
using DemoMVC.Models.ViewModels;
using IEmailSender = DemoMVC.Services.IEmailSender;

namespace DemoMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        // --- ĐĂNG NHẬP ---
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
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Student");
            }
            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Bạn cần xác thực email trước khi đăng nhập.");
                return View(model);
            }
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản tạm khóa do nhập sai nhiều lần. Vui lòng thử lại sau 5 phút.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        // --- ĐĂNG KÝ & GỬI EMAIL XÁC THỰC ---
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Action(
                    "ConfirmEmail", "Account",
                    new { userId = user.Id, code = code },
                    protocol: Request.Scheme);

                try
                {
                    await _emailSender.SendEmailAsync(
                        model.Email,
                        "Xác thực tài khoản DemoMVC",
                        $"Chào {model.FullName},<br/>Vui lòng bấm <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>vào đây</a> để kích hoạt tài khoản.");

                    ViewBag.Message = "Đăng ký thành công! Vui lòng kiểm tra hộp thư Email để xác thực tài khoản trước khi đăng nhập.";
                }
                catch (Exception ex)
                {
                    ViewBag.Message = $"Tạo tài khoản thành công nhưng gửi email thất bại ({ex.Message}). Vui lòng kiểm tra cấu hình SMTP.";
                }

                return View("Notification");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null) return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);

            ViewBag.Message = result.Succeeded
                ? "Xác thực Email thành công! Bạn đã có thể đăng nhập vào hệ thống."
                : "Lỗi xác thực Email. Link có thể đã hết hạn.";
            return View("Notification");
        }

        // --- QUÊN MẬT KHẨU ---
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email không tồn tại trong hệ thống.");
                return View(model);
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            return View("ResetPassword", new ResetPasswordViewModel
            {
                Email = model.Email,
                Code = code
            });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? code = null, string? email = null)
        {
            if (code == null) return BadRequest("Thiếu mã xác thực.");
            return View(new ResetPasswordViewModel { Code = code, Email = email ?? string.Empty });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản với Email này.");
                return View(model);
            }

            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
            var result = await _userManager.ResetPasswordAsync(user, code, model.Password);
            if (result.Succeeded)
            {
                ViewBag.Message = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập với mật khẩu mới.";
                return View("Notification");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // --- ĐĂNG XUẤT & TỪ CHỐI TRUY CẬP ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}