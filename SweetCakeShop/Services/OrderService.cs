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

            var vipDiscount =
                await _customerLoyaltyService.CalculateDiscountAsync(
                    userId,
                    productSubtotal);

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

                // VIP giảm 10% tiền sản phẩm, không giảm phí giao hàng
                TotalPrice = productSubtotal - vipDiscount + checkout.ShippingFee,

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
