namespace SweetCakeShop.Configurations
{
    public sealed class VnPaySettings
    {
        public string TmnCode { get; set; } = string.Empty;

        public string HashSecret { get; set; } = string.Empty;

        public string PaymentUrl { get; set; }
            = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

        // De trong de ung dung tu tao theo domain dang mo.
        // Khi dung Cloudflare Quick Tunnel, nen dien URL public co dinh cho moi lan chay.
        public string ReturnUrl { get; set; } = string.Empty;

        public string Version { get; set; } = "2.1.0";

        public string Command { get; set; } = "pay";

        public string CurrCode { get; set; } = "VND";

        public string Locale { get; set; } = "vn";

        public string OrderType { get; set; } = "other";

        public int ExpireMinutes { get; set; } = 15;
    }
}
