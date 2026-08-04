namespace SweetCakeShop.Models
{
    public class Coupon
    {
        public int CouponId { get; set; }
        public string Code { get; set; } = string.Empty; // Mã coupon (unique)
        public decimal DiscountPercent { get; set; } // % giảm (0-100)
        public string CustomerType { get; set; } = "VIP"; // VIP hoặc Regular
        public bool IsActive { get; set; } = true;
        public DateTime? ExpiryDate { get; set; } // Ngày hết hạn
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        // Relationships
        public virtual ICollection<CouponCustomer> CouponCustomers { get; set; } = new List<CouponCustomer>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
