// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace SweetCakeShop.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public EmailModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool IsEmailConfirmed { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "New email")]
            public string NewEmail { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            Input = new InputModel
            {
                NewEmail = email,
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != email)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmailChange",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, email = Input.NewEmail, code = code },
                    protocol: Request.Scheme);
                await _emailSender.SendEmailAsync(
                    Input.NewEmail,
                    "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                StatusMessage = "Confirmation link to change email sent. Please check your email.";
                return RedirectToPage();
            }

            StatusMessage = "Your email is unchanged.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound(
                    $"Không tìm thấy người dùng có ID '{_userManager.GetUserId(User)}'.");
            }

            var email = await _userManager.GetEmailAsync(user);

            if (string.IsNullOrWhiteSpace(email))
            {
                StatusMessage = "Error: Tài khoản chưa có địa chỉ email.";
                return RedirectToPage();
            }

            var isConfirmed =
                await _userManager.IsEmailConfirmedAsync(user);

            if (isConfirmed)
            {
                StatusMessage = "Email này đã được xác nhận trước đó.";
                return RedirectToPage();
            }

            try
            {
                var userId =
                    await _userManager.GetUserIdAsync(user);

                var code =
                    await _userManager.GenerateEmailConfirmationTokenAsync(user);

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
                    StatusMessage =
                        "Error: Không thể tạo liên kết xác nhận email.";

                    return RedirectToPage();
                }

                var safeCallbackUrl =
                    HtmlEncoder.Default.Encode(callbackUrl);

                var htmlMessage = $"""
        <!DOCTYPE html>
        <html lang="vi">
        <body style="
            margin:0;
            padding:30px;
            background-color:#fff7fa;
            font-family:Arial,sans-serif;">

            <div style="
                max-width:600px;
                margin:auto;
                background-color:#ffffff;
                border-radius:20px;
                overflow:hidden;
                box-shadow:0 8px 25px rgba(0,0,0,0.08);">

                <div style="
                    height:7px;
                    background:linear-gradient(
                        90deg,
                        #f06292,
                        #d81b60);">
                </div>

                <div style="padding:35px;text-align:center;">

                    <div style="
                        width:85px;
                        height:85px;
                        line-height:85px;
                        margin:0 auto 20px;
                        border-radius:50%;
                        background-color:#fff0f5;
                        font-size:42px;">
                        ✉️
                    </div>

                    <h2 style="color:#d81b60;">
                        Xác nhận email
                    </h2>

                    <p style="
                        color:#555;
                        line-height:1.7;
                        text-align:left;">

                        Xin chào,<br /><br />

                        Hãy nhấn vào nút bên dưới để xác nhận
                        địa chỉ email của tài khoản SweetCakeShop.
                    </p>

                    <p style="margin:30px 0;">
                        <a href="{safeCallbackUrl}"
                           style="
                               display:inline-block;
                               background-color:#d81b60;
                               color:#ffffff;
                               padding:14px 28px;
                               border-radius:12px;
                               text-decoration:none;
                               font-weight:bold;">

                            Xác nhận email
                        </a>
                    </p>

                    <p style="
                        color:#777;
                        font-size:14px;
                        line-height:1.6;
                        text-align:left;">

                        Nếu bạn không thực hiện yêu cầu này,
                        bạn có thể bỏ qua email.
                    </p>

                    <hr style="
                        border:none;
                        border-top:1px solid #eeeeee;
                        margin:25px 0;" />

                    <p style="color:#999;font-size:13px;">
                        SweetCakeShop
                    </p>

                </div>
            </div>
        </body>
        </html>
        """;

                await _emailSender.SendEmailAsync(
                    email,
                    "Xác nhận email - SweetCakeShop",
                    htmlMessage);

                StatusMessage =
                    "Đã gửi email xác nhận. Hãy kiểm tra Hộp thư đến, Spam hoặc Quảng cáo.";

                return RedirectToPage();
            }
            catch
            {
                StatusMessage =
                    "Error: Không thể gửi email xác nhận. Vui lòng thử lại.";

                return RedirectToPage();
            }
        }
    }
}
