using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace SweetCakeShop.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string CustomerPhone { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public bool IsGuest { get; set; } = true;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = "COD";

        public IdentityUser? User { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; }
            = new List<OrderDetail>();


        //=========================
        // GHN
        //=========================

        /// <summary>
        /// Mã vận đơn GHN
        /// </summary>
        public string? GhnOrderCode { get; set; }

        /// <summary>
        /// Link Tracking
        /// </summary>
        public string? TrackingUrl { get; set; }

        /// <summary>
        /// Phí vận chuyển
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;

        /// <summary>
        /// ProvinceID của GHN
        /// </summary>
        public int? ProvinceId { get; set; }

        /// <summary>
        /// DistrictID của GHN
        /// </summary>
        public int? DistrictId { get; set; }

        /// <summary>
        /// WardCode của GHN
        /// </summary>
        public string? WardCode { get; set; }

        /// <summary>
        /// Tên tỉnh
        /// </summary>
        public string? Province { get; set; }

        /// <summary>
        /// Tên huyện
        /// </summary>
        public string? District { get; set; }

        /// <summary>
        /// Tên xã
        /// </summary>
        public string? Ward { get; set; }

        /// <summary>
        /// service_id GHN
        /// </summary>
        public int? ServiceId { get; set; }

        /// <summary>
        /// Trạng thái GHN
        /// </summary>
        public string? ShippingStatus { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }

        public DateTime? DeliveredDate { get; set; }
    }
}