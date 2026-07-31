using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using SweetCakeShop.Configurations;

namespace SweetCakeShop.Services
{
    public class GmailEmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<GmailEmailSender> _logger;

        public GmailEmailSender(
            IOptions<EmailSettings> emailSettings,
            ILogger<GmailEmailSender> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(
            string email,
            string subject,
            string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    "Địa chỉ email nhận không được để trống.",
                    nameof(email));
            }

            if (string.IsNullOrWhiteSpace(_emailSettings.SenderEmail))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình EmailSettings:SenderEmail.");
            }

            if (string.IsNullOrWhiteSpace(_emailSettings.SenderPassword))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình EmailSettings:SenderPassword.");
            }

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _emailSettings.SenderName,
                    _emailSettings.SenderEmail));

            message.To.Add(MailboxAddress.Parse(email));

            message.Subject = subject;

            message.Body = new TextPart(TextFormat.Html)
            {
                Text = htmlMessage
            };

            using var smtpClient = new SmtpClient();

            try
            {
                await smtpClient.ConnectAsync(
                    _emailSettings.Host,
                    _emailSettings.Port,
                    SecureSocketOptions.StartTls);

                await smtpClient.AuthenticateAsync(
                    _emailSettings.SenderEmail,
                    _emailSettings.SenderPassword);

                await smtpClient.SendAsync(message);

                await smtpClient.DisconnectAsync(true);

                _logger.LogInformation(
                    "Đã gửi email thành công đến {Email}.",
                    email);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Gửi email đến {Email} thất bại.",
                    email);

                throw;
            }
        }
    }
}