using SweetCakeShop.Models;
using SweetCakeShop.Services;

namespace SweetCakeShop.Models.ViewModels
{
    public class PaymentViewModel
    {
        public Order? Order { get; set; }
        public PaymentResult? PaymentResult { get; set; }
    }
}