// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace SweetCakeShop.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

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
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    // Không tiết lộ email có tồn tại hay không
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);


                await _emailSender.SendEmailAsync(
    Input.Email,
    "Đặt lại mật khẩu - SweetCakeShop",
    $"""
    <!DOCTYPE html>
    <html lang="vi">
    <body style="font-family:Arial,sans-serif;background:#fff7fa;padding:30px;">
        <div style="max-width:600px;margin:auto;background:white;
                    border-radius:18px;padding:30px;
                    box-shadow:0 6px 20px rgba(0,0,0,0.08);">

            <h2 style="color:#d81b60;">
                Đặt lại mật khẩu
            </h2>

            <p>
                SweetCakeShop nhận được yêu cầu đặt lại mật khẩu
                cho tài khoản của bạn.
            </p>

            <p>
                Nhấn vào nút bên dưới để tạo mật khẩu mới:
            </p>

            <p style="text-align:center;margin:30px 0;">
                <a href="{HtmlEncoder.Default.Encode(callbackUrl)}"
                   style="display:inline-block;
                          background:#d81b60;
                          color:white;
                          padding:13px 25px;
                          border-radius:10px;
                          text-decoration:none;
                          font-weight:bold;">
                    Đặt lại mật khẩu
                </a>
            </p>

            <p style="color:#777;font-size:14px;">
                Nếu bạn không yêu cầu đặt lại mật khẩu,
                hãy bỏ qua email này.
            </p>

            <hr style="border:none;border-top:1px solid #eee;" />

            <p style="color:#999;font-size:13px;">
                SweetCakeShop
            </p>
        </div>
    </body>
    </html>
    """);
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
