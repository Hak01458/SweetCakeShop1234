using Microsoft.AspNetCore.Identity;

namespace SweetCakeShop.Models
{
    public class CouponCustomer
    {
        public int CouponCustomerId { get; set; }
        public int CouponId { get; set; }
        public string CustomerId { get; set; } = string.Empty; // FK to IdentityUser
        public DateTime AssignedDate { get; set; } = DateTime.Now;
        public bool IsUsed { get; set; } = false; // Đã sử dụng chưa
        public DateTime? UsedDate { get; set; } // Ngày sử dụng

        // Relationships
        public virtual Coupon? Coupon { get; set; }
        public virtual IdentityUser? Customer { get; set; }
    }
}
