using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public interface IPaymentService
    {
        // Create a provider session and return PaymentResult (contains a PaymentUrl to redirect the browser)
        Task<PaymentResult> CreatePaymentAsync(Order order, string successUrl, string cancelUrl);

        // Backwards-compatible helper (not used for Stripe flow)
        Task<string> CreatePaymentRedirectUrlAsync(Order order, string returnUrl);
    }
}
