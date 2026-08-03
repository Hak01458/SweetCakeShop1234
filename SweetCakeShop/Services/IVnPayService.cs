using Microsoft.AspNetCore.Http;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(
            Order order,
            HttpContext httpContext,
            string fallbackReturnUrl,
            string? bankCode = null);

        VnPayCallbackResult ReadCallback(IQueryCollection query);
    }

    public sealed class VnPayCallbackResult
    {
        public bool IsValidSignature { get; init; }

        public bool IsSuccess { get; init; }

        public int? OrderId { get; init; }

        public decimal Amount { get; init; }

        public string TransactionReference { get; init; } = string.Empty;

        public string TransactionNumber { get; init; } = string.Empty;

        public string ResponseCode { get; init; } = string.Empty;

        public string TransactionStatus { get; init; } = string.Empty;

        public string BankCode { get; init; } = string.Empty;

        public string PayDate { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;
    }
}
