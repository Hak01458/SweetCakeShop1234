using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;

namespace SweetCakeShop.Services
{
    public class CustomerLoyaltyService
    {
        // Cần 5 đơn đã giao để trở thành VIP
        public const int VipRequiredDeliveredOrders = 5;

        // VIP được giảm 10% tiền sản phẩm
        public const decimal VipDiscountRate = 0.15m;

        private readonly ApplicationDbContext _db;

        public CustomerLoyaltyService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Đếm số đơn đã giao của khách hàng
        public async Task<int> GetDeliveredOrderCountAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return 0;
            }

            return await _db.Orders
                .AsNoTracking()
                .CountAsync(o =>
                    o.UserId == userId &&
                    o.Status == "Delivered");
        }

        // Kiểm tra khách hàng có phải VIP không
        public async Task<bool> IsVipAsync(string? userId)
        {
            var deliveredOrderCount =
                await GetDeliveredOrderCountAsync(userId);

            return deliveredOrderCount >= VipRequiredDeliveredOrders;
        }

        // Tính số tiền được giảm
        public async Task<decimal> CalculateDiscountAsync(
            string? userId,
            decimal productSubtotal)
        {
            var isVip = await IsVipAsync(userId);

            if (!isVip)
            {
                return 0;
            }

            return decimal.Round(
                productSubtotal * VipDiscountRate,
                0,
                MidpointRounding.AwayFromZero);
        }
    }
}