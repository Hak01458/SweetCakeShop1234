using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Services;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Stripe.Checkout;
using Stripe;
using System.Threading.Tasks;

namespace SweetCakeShop.Controllers
{
    public class CartController : Controller
    {
        private readonly CustomerLoyaltyService _customerLoyaltyService;
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private readonly OrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly GhnService _ghnService;
        private readonly OrderEmailService _orderEmailService;
        public CartController(
    ApplicationDbContext context,
    CartService cartService,
    OrderService orderService,
    IPaymentService paymentService,
    GhnService ghnService,
    CustomerLoyaltyService customerLoyaltyService,
    OrderEmailService orderEmailService)
        {
            _context = context;
            _cartService = cartService;
            _orderService = orderService;
            _paymentService = paymentService;
            _ghnService = ghnService;
            _customerLoyaltyService = customerLoyaltyService;
            _orderEmailService = orderEmailService;
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            return View(cart);
        }

        [HttpPost]
        public IActionResult Add(int id, int quantity = 1)
        {
            var product = _context.Products.Find(id);
            if (product == null)
                return NotFound();

            _cartService.AddToCart(product, quantity);

            return Json(new { success = true, message = $"{product.ProductName} đã thêm vào giỏ hàng!" });
        }

        [HttpPost]
        public IActionResult Update(int productId, int quantity)
        {
            _cartService.UpdateQuantity(productId, quantity);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            _cartService.RemoveFromCart(productId);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Count()
        {
            var count = _cartService.GetCart().Items.Sum(i => i.Quantity);
            return Json(new { count });
        }

        // Show checkout with shipping form
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = _cartService.GetCart();

            if (!cart.Items.Any())
            {
                return RedirectToAction("Index");
            }

            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["LoginMessage"] =
                    "Bạn phải tiến hành đăng nhập để tiếp tục mua sản phẩm";

                var returnUrl = Url.Action("Checkout", "Cart");

                return Redirect(
                    $"/Identity/Account/Login?returnUrl=" +
                    System.Net.WebUtility.UrlEncode(returnUrl ?? "/"));
            }

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var deliveredOrderCount =
                await _customerLoyaltyService
                    .GetDeliveredOrderCountAsync(userId);

            var isVip =
                deliveredOrderCount >=
                CustomerLoyaltyService.VipRequiredDeliveredOrders;

            ViewBag.DeliveredOrderCount = deliveredOrderCount;
            ViewBag.IsVip = isVip;
            ViewBag.VipDiscountRate =
                CustomerLoyaltyService.VipDiscountRate;

            var model = new CheckoutViewModel
            {
                CustomerEmail =
                    User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,

                CustomerName =
                    User.Identity?.Name ?? string.Empty
            };

            ViewData["Cart"] = cart;

            return View(model);
        }
        // Accept checkout from guests and authenticated users
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutConfirm(CheckoutViewModel checkout)
        {
            var cart = _cartService.GetCart();
            if (!cart.Items.Any())  
                return RedirectToAction("Index");

            string? userId = null;
            if (User.Identity?.IsAuthenticated == true)
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _orderService.CreateOrderAsync(cart, checkout, userId);

            _cartService.ClearCart();

            // After creating order, redirect to Payment selection page
            return RedirectToAction("Payment", new { orderId = order.OrderId });
        }

        // Payment selection & result page
        [HttpGet]
        public async Task<IActionResult> Payment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            var model = new PaymentViewModel
            {
                Order = order
            };

            return View(model);
        }

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
                order.Status = "COD";

                await _context.SaveChangesAsync();

                await _orderEmailService
                    .SendOrderConfirmationAsync(order.OrderId);

                return RedirectToAction(
                    "Success",
                    new { orderId = order.OrderId });
            }
            else if (method == "Online")
            {
                // Build success/cancel URLs that Stripe will redirect to.
                // Use Stripe's placeholder {CHECKOUT_SESSION_ID} so we can verify the session on return.
                var baseSuccessUrl = Url.Action("Success", "Cart", new { orderId = order.OrderId }, Request.Scheme) ?? string.Empty;
                var successUrl = baseSuccessUrl + (baseSuccessUrl.Contains("?") ? "&session_id={CHECKOUT_SESSION_ID}" : "?session_id={CHECKOUT_SESSION_ID}");
                var cancelUrl = Url.Action("Payment", "Cart", new { orderId = order.OrderId }, Request.Scheme) ?? string.Empty;

                // Create Stripe Checkout Session via service (provides session.Url)
                var payment = await _paymentService.CreatePaymentAsync(order, successUrl, cancelUrl);

                // If payment service reports success (Stripe session created or other gateway),
                // set status to Confirmed. Otherwise mark COD.
                order.Status =
    payment.Success
        ? "PendingPayment"
        : "PaymentFailed";
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(payment.PaymentUrl))
                {
                    return Redirect(payment.PaymentUrl); // send browser to Stripe Checkout
                }

                // fallback: show Payment view with bank-transfer info
                var model = new PaymentViewModel
                {
                    Order = order,
                    PaymentResult = payment
                };

                return View("Payment", model);
            }

            // unexpected method
            TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
            return RedirectToAction("Payment", new { orderId = order.OrderId });
        }

        // Internal page that displays your payment image/QR code in the middle
        [HttpGet]
        public async Task<IActionResult> OnlinePayment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            var model = new PaymentViewModel
            {
                Order = order
            };

            return View(model); // Views/Cart/OnlinePayment.cshtml
        }

        // User clicks "I have paid" on internal page to confirm manually
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOnlinePayment(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                return NotFound();

            // mark as awaiting manual confirmation (you can change to Confirmed if you prefer)
            order.Status = "Confirmed";

            await _context.SaveChangesAsync();

            await _orderEmailService
                .SendOrderConfirmationAsync(order.OrderId);

            return RedirectToAction(
                "Success",
                new { orderId = order.OrderId });
        }

        // Success: can be reached from Stripe redirect (contains session_id) or internal flows.
        [HttpGet]
        public async Task<IActionResult> Success(int orderId, string? session_id)
        {
            var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            // If Stripe returned a session_id, verify payment status server-side (recommended)
            if (!string.IsNullOrEmpty(session_id))
            {
                try
                {
                    var sessionService = new SessionService();

                    var session =
                        await sessionService.GetAsync(session_id);

                    if (session != null &&
                        session.PaymentStatus == "paid")
                    {
                        order.Status = "Confirmed";

                        await _context.SaveChangesAsync();

                        await _orderEmailService
                            .SendOrderConfirmationAsync(order.OrderId);
                    }
                    else
                    {
                        order.Status = "PaymentFailed";

                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Lỗi kiểm tra Stripe: {ex.Message}");
                }
            }

            return View(order);
        }
        [HttpGet]
        public async Task<IActionResult> CalculateShippingFee(int districtId, string wardCode)
        {
            try
            {
                var fee = await _ghnService.CalculateFeeAsync(districtId, wardCode);

                return Ok(new
                {
                    success = true,
                    shippingFee = fee
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> TestLeadTime(int districtId, string wardCode)
        {
            try
            {
                var leadTime = await _ghnService.GetLeadTimeAsync(districtId, wardCode);

                return Ok(new
                {
                    success = true,
                    leadTime = leadTime
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> TestDistricts()
        {
            // ProvinceID của Hồ Chí Minh
            var districts = await _ghnService.GetDistrictsAsync(202);

            return Ok(districts);
        }
        [HttpGet]
        public async Task<IActionResult> TestWards()
        {
            // Quận Tân Bình
            var wards = await _ghnService.GetWardsAsync(1455);

            return Ok(wards);
        }
    }
}