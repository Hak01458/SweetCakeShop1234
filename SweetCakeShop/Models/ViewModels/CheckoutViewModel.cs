namespace SweetCakeShop.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public decimal ShippingFee { get; set; }

        // Optional: you can add shipping method, notes, payment method etc.
    }
}
