#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace SweetCakeShop.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ResendEmailConfirmationModel> _logger;

        public ResendEmailConfirmationModel(
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            ILogger<ResendEmailConfirmationModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusType { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        public void OnGet(string email = null)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                Input.Email = email;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var email = Input.Email.Trim();

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                StatusMessage =
                    "Không tìm thấy tài khoản nào đăng ký bằng email này.";

                StatusType = "error";

                return RedirectToPage();
            }

            var emailConfirmed =
                await _userManager.IsEmailConfirmedAsync(user);

            if (emailConfirmed)
            {
                StatusMessage =
                    "Email này đã được xác nhận. Bạn có thể đăng nhập ngay.";

                StatusType = "info";

                return RedirectToPage();
            }

            try
            {
                var userId =
                    await _userManager.GetUserIdAsync(user);

                var code =
                    await _userManager
                        .GenerateEmailConfirmationTokenAsync(user);

                code = WebEncoders.Base64UrlEncode(
                    Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new
                    {
                        area = "Identity",
                        userId,
                        code
                    },
                    protocol: Request.Scheme);

                if (string.IsNullOrWhiteSpace(callbackUrl))
                {
                    throw new InvalidOperationException(
                        "Không thể tạo đường dẫn xác nhận email.");
                }

                var safeCallbackUrl =
                    HtmlEncoder.Default.Encode(callbackUrl);

                var htmlMessage = $"""
                <!DOCTYPE html>
                <html lang="vi">
                <body style="font-family: Arial, sans-serif;
                             background-color: #fff7fa;
                             padding: 30px;">

                    <div style="max-width: 600px;
                                margin: auto;
                                background-color: white;
                                padding: 30px;
                                border-radius: 18px;
                                box-shadow: 0 5px 20px rgba(0,0,0,0.08);">

                        <h2 style="color: #d81b60;">
                            Xác nhận tài khoản SweetCakeShop
                        </h2>

                        <p>
                            Bạn vừa yêu cầu gửi lại liên kết xác nhận email.
                        </p>

                        <p>
                            Nhấn vào nút bên dưới để xác nhận tài khoản:
                        </p>

                        <p style="text-align: center;
                                  margin: 30px 0;">

                            <a href="{safeCallbackUrl}"
                               style="display: inline-block;
                                      background-color: #d81b60;
                                      color: white;
                                      padding: 13px 25px;
                                      border-radius: 10px;
                                      text-decoration: none;
                                      font-weight: bold;">

                                Xác nhận email
                            </a>
                        </p>

                        <p style="color: #777;
                                  font-size: 14px;">

                            Nếu bạn không yêu cầu email này,
                            bạn có thể bỏ qua.
                        </p>
                    </div>
                </body>
                </html>
                """;

                await _emailSender.SendEmailAsync(
                    email,
                    "Xác nhận tài khoản SweetCakeShop",
                    htmlMessage);

                StatusMessage =
                    "Đã gửi email xác nhận. Hãy kiểm tra Hộp thư đến, Spam và Quảng cáo.";

                StatusType = "success";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Không thể gửi lại email xác nhận cho {Email}.",
                    email);

                StatusMessage =
                    "Không thể gửi email xác nhận. Hãy kiểm tra cấu hình Gmail hoặc thử lại.";

                StatusType = "error";

                return RedirectToPage();
            }
        }
    }
}