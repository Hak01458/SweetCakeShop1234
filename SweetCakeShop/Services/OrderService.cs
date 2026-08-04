using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _db;
        private readonly GhnService _ghnService;
        private readonly CustomerLoyaltyService _customerLoyaltyService;

        public OrderService(
            ApplicationDbContext db,
            GhnService ghnService,
            CustomerLoyaltyService customerLoyaltyService)
        {
            _db = db;
            _ghnService = ghnService;
            _customerLoyaltyService = customerLoyaltyService;
        }

        public async Task<Order> CreateOrderAsync(
    CartViewModel cart,
    CheckoutViewModel checkout,
    string? userId)
        {
            if (cart == null || !cart.Items.Any())
            {
                throw new ArgumentException("Cart is empty", nameof(cart));
            }

            var productSubtotal = cart.TotalAmount;

            // Apply coupon discount if provided
            decimal couponDiscount = 0;
            int? couponId = null;

            if (checkout.CouponId.HasValue && !string.IsNullOrEmpty(userId))
            {
                var couponCustomer = await _db.CouponCustomers
                    .Include(cc => cc.Coupon)
                    .FirstOrDefaultAsync(cc => cc.CouponCustomerId == checkout.CouponId.Value
                        && cc.CustomerId == userId
                        && !cc.IsUsed
                        && cc.Coupon != null
                        && cc.Coupon.IsActive);

                if (couponCustomer?.Coupon != null)
                {
                    // Check expiry
                    if (!couponCustomer.Coupon.ExpiryDate.HasValue || 
                        couponCustomer.Coupon.ExpiryDate.Value.Date >= DateTime.Today)
                    {
                        couponDiscount = decimal.Round(
                            productSubtotal * (couponCustomer.Coupon.DiscountPercent / 100m),
                            0,
                            MidpointRounding.AwayFromZero);
                        couponId = couponCustomer.Coupon.CouponId;
                    }
                }
            }

            var order = new Order
            {
                UserId = userId ?? string.Empty,

                CustomerName = checkout.CustomerName ?? string.Empty,
                CustomerEmail = checkout.CustomerEmail ?? string.Empty,
                CustomerPhone = checkout.CustomerPhone ?? string.Empty,
                ShippingAddress = checkout.ShippingAddress ?? string.Empty,

                ProvinceId = checkout.ProvinceId,
                DistrictId = checkout.DistrictId,
                WardCode = checkout.WardCode,

                Province = checkout.Province,
                District = checkout.District,
                Ward = checkout.Ward,

                ShippingFee = checkout.ShippingFee,

                IsGuest = string.IsNullOrEmpty(userId),
                OrderDate = DateTime.Now,

                // Coupon discount, không giảm phí giao hàng
                TotalPrice = productSubtotal - couponDiscount + checkout.ShippingFee,

                CouponId = couponId,

                Status = "COD"
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var item in cart.Items)
            {
                var detail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };

                _db.OrderDetails.Add(detail);
            }

            await _db.SaveChangesAsync();

            return order;
        }
    }
}
