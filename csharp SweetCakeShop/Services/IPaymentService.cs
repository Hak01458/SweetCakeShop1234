using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public interface IPaymentService
    {
        Task<PaymentResult> CreatePaymentAsync(Order order);
        // New: create redirect URL to third-party payment page (return full external URL)
        Task<string> CreatePaymentRedirectUrlAsync(Order order, string returnUrl);
    }
}