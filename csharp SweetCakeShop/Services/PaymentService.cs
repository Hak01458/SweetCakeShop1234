using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public class PaymentResult
    {
        public string PaymentCode { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
    }

    public class PaymentService : IPaymentService
    {
        private readonly HttpClient? _http;
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _configuration;

        public PaymentService(HttpClient? http, IConfiguration configuration, ILogger<PaymentService> logger)
        {
            _http = http;
            _configuration = configuration;
            _logger = logger;
        }

        // Existing method (kept for compatibility) — can be local generator or call provider if desired.
        public Task<PaymentResult> CreatePaymentAsync(Order order)
        {
            // Keep simple fallback (bank code generator) so any existing usage still works
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var paymentCode = $"BANK-{order.OrderId}-{timestamp}";

                var bankName = "Ngân hàng ABC";
                var accountNumber = "123456789";
                var accountName = "SweetCakeShop";
                var transferNote = paymentCode;

                var message =
                    $"Vui lòng chuyển khoản theo thông tin sau:\n" +
                    $"- Ngân hàng: {bankName}\n" +
                    $"- Số tài khoản: {accountNumber}\n" +
                    $"- Chủ tài khoản: {accountName}\n" +
                    $"- Số tiền: {order.TotalPrice.ToString("N0")} ₫\n" +
                    $"- Nội dung chuyển khoản: {transferNote}\n\n" +
                    "Sau khi chuyển khoản, vui lòng chờ xác nhận từ hệ thống. Mã thanh toán ở trên dùng để đối soát.";

                var result = new PaymentResult
                {
                    Success = true,
                    PaymentCode = paymentCode,
                    PaymentUrl = string.Empty,
                    Amount = order.TotalPrice,
                    Message = message
                };

                _logger.LogInformation("Generated bank transfer code {PaymentCode} for order {OrderId}", paymentCode, order.OrderId);

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate bank transfer code for order {OrderId}", order.OrderId);
                return Task.FromResult(new PaymentResult
                {
                    Success = false,
                    Message = "Không thể tạo mã thanh toán. Vui lòng thử lại hoặc liên hệ hỗ trợ.",
                    PaymentCode = string.Empty,
                    Amount = order.TotalPrice
                });
            }
        }

        // New: create external redirect URL to provider page.
        public Task<string> CreatePaymentRedirectUrlAsync(Order order, string returnUrl)
        {
            // Use configured provider base URL
            var providerBase = _configuration["PaymentGateway:ProviderUrl"] ?? _http?.BaseAddress?.ToString() ?? "https://example-payment-provider.test";

            // Example redirect URL format expected by provider:
            // {providerBase}/pay?orderId=...&amount=...&returnUrl=...
            // Adjust parameter names to match provider's requirements.
            var sb = new System.Text.StringBuilder();
            sb.Append(providerBase.TrimEnd('/'));
            sb.Append("/pay?");
            sb.Append("orderId=").Append(WebUtility.UrlEncode(order.OrderId.ToString()));
            sb.Append("&amount=").Append(WebUtility.UrlEncode(order.TotalPrice.ToString()));
            sb.Append("&currency=").Append(WebUtility.UrlEncode("VND"));
            sb.Append("&customerName=").Append(WebUtility.UrlEncode(order.CustomerName ?? string.Empty));
            sb.Append("&customerEmail=").Append(WebUtility.UrlEncode(order.CustomerEmail ?? string.Empty));
            sb.Append("&returnUrl=").Append(WebUtility.UrlEncode(returnUrl ?? string.Empty));

            var redirectUrl = sb.ToString();

            _logger.LogInformation("Built payment redirect URL for order {OrderId}: {Url}", order.OrderId, redirectUrl);

            return Task.FromResult(redirectUrl);
        }
    }
}