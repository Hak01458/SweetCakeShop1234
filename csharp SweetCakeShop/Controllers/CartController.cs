using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Services;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Net;

namespace SweetCakeShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private readonly OrderService _orderService;
        private readonly IPaymentService _paymentService;

        public CartController(ApplicationDbContext context, CartService cartService, OrderService orderService, IPaymentService paymentService)
        {
            _context = context;
            _cartService = cartService;
            _orderService = orderService;
            _paymentService = paymentService;
        }

        // ... other actions unchanged ...

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int orderId, string method)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            if (method == "COD")
            {
                order.Status = "Confirmed";
                await _context.SaveChangesAsync();

                return RedirectToAction("Success", new { orderId = order.OrderId });
            }
            else if (method == "Online")
            {
                // Build return URL that provider will call back to
                var callbackUrl = Url.Action("PaymentCallback", "Cart", new { orderId = order.OrderId }, Request.Scheme);

                // Create redirect URL to provider page (external)
                var providerUrl = await _paymentService.CreatePaymentRedirectUrlAsync(order, callbackUrl);

                // Mark order awaiting payment
                order.Status = "RedirectedToProvider";
                await _context.SaveChangesAsync();

                // Redirect browser to provider page
                return Redirect(providerUrl);
            }

            TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
            return RedirectToAction("Payment", new { orderId = order.OrderId });
        }

        // Provider will redirect back here after payment creation/completion
        // Example: /Cart/PaymentCallback?orderId=123&paymentCode=...&status=success
        [HttpGet]
        public async Task<IActionResult> PaymentCallback(int orderId, string? paymentCode, string? status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            // Interpret status from provider and update order
            if (!string.IsNullOrEmpty(status) && status.Equals("success", StringComparison.OrdinalIgnoreCase))
            {
                order.Status = "Confirmed";
            }
            else if (!string.IsNullOrEmpty(status) && status.Equals("pending", StringComparison.OrdinalIgnoreCase))
            {
                order.Status = "AwaitingPayment";
            }
            else
            {
                order.Status = "PaymentFailed";
            }

            // Optionally persist paymentCode somewhere — for now store in TempData to show in view
            await _context.SaveChangesAsync();

            var paymentResult = new PaymentResult
            {
                Success = status != null && status.Equals("success", StringComparison.OrdinalIgnoreCase),
                PaymentCode = paymentCode ?? string.Empty,
                PaymentUrl = string.Empty,
                Amount = order.TotalPrice,
                Message = status != null ? $"Kết quả thanh toán: {status}" : "Không có trạng thái từ provider"
            };

            var model = new PaymentViewModel
            {
                Order = order,
                PaymentResult = paymentResult
            };

            return View("Payment", model);
        }

        // Success action unchanged...
    }
}