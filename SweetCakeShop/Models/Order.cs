using Microsoft.AspNetCore.Identity;
namespace SweetCakeShop.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        // If the order was placed by an authenticated user, this will be populated.
        // For guest checkout this stays empty.
        public string UserId { get; set; } = string.Empty;

        // Shipping / customer fields (for guest checkout and for records)
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public bool IsGuest { get; set; } = true;

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "COD";     // COD, Confirmed, Baked, Delivered, Cancelled

        public IdentityUser? User { get; set; }           // nếu dùng Identity
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public string? GhnOrderCode { get; set; }      // Mã vận đơn GHN trả về
        public string? TrackingUrl { get; set; }        // Link tracking cho khách
        public decimal ShippingFee { get; set; } = 0;  // Phí ship tính từ GHN
        public string? Province { get; set; }          // Tỉnh/thành (GHN cần mã)
        public string? District { get; set; }          // Quận/huyện
        public string? Ward { get; set; }              // Phường/xã
    }
}
