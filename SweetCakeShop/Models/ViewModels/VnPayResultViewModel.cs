using SweetCakeShop.Models;
using SweetCakeShop.Services;

namespace SweetCakeShop.Models.ViewModels
{
    public sealed class VnPayResultViewModel
    {
        public Order? Order { get; set; }

        public VnPayCallbackResult Result { get; set; } = new();

        public string DisplayMessage { get; set; } = string.Empty;
    }
}
