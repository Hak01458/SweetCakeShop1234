namespace SweetCakeShop.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string CustomerPhone { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        // Tên địa chỉ để lưu vào Order
        public string? Province { get; set; }

        public string? District { get; set; }

        public string? Ward { get; set; }

        // ID của GHN để tính phí và tạo vận đơn
        public int ProvinceId { get; set; }

        public int DistrictId { get; set; }

        public string WardCode { get; set; } = string.Empty;

        public decimal ShippingFee { get; set; }
    }
}