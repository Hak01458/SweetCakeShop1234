using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _db;

        public OrderService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Order> CreateOrderAsync(CartViewModel cart, CheckoutViewModel checkout, string? userId)
        {
            if (cart == null || !cart.Items.Any())
                throw new ArgumentException("Cart is empty", nameof(cart));

            var order = new Order
            {
                UserId = userId ?? string.Empty,
                CustomerName = checkout.CustomerName ?? string.Empty,
                CustomerEmail = checkout.CustomerEmail ?? string.Empty,
                CustomerPhone = checkout.CustomerPhone ?? string.Empty,
                ShippingAddress = checkout.ShippingAddress ?? string.Empty,
                Province = checkout.Province,
                District = checkout.District,
                Ward = checkout.Ward,
                ShippingFee = checkout.ShippingFee,
                IsGuest = string.IsNullOrEmpty(userId),
                OrderDate = DateTime.UtcNow,
                TotalPrice = cart.TotalAmount,
                Status = "COD"
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(); // get OrderId

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

            // load navigation if needed
            await _db.Entry(order).Collection(o => o.OrderDetails).LoadAsync();

            return order;
        }
    }
}
