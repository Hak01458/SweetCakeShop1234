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
using System.Text.Json;

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
        private readonly IVnPayService _vnPayService;
        public CartController(
    ApplicationDbContext context,
    CartService cartService,
    OrderService orderService,
    IPaymentService paymentService,
    GhnService ghnService,
    CustomerLoyaltyService customerLoyaltyService,
    OrderEmailService orderEmailService,
    IVnPayService vnPayService)
        {
            _context = context;
            _cartService = cartService;
            _orderService = orderService;
            _paymentService = paymentService;
            _ghnService = ghnService;
            _customerLoyaltyService = customerLoyaltyService;
            _orderEmailService = orderEmailService;
            _vnPayService = vnPayService;
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

            // Validate coupon if provided and user is VIP
            if (checkout.CouponId.HasValue && !string.IsNullOrEmpty(userId))
            {
                var couponCustomer = await _context.CouponCustomers
                    .Include(cc => cc.Coupon)
                    .FirstOrDefaultAsync(cc => cc.CouponCustomerId == checkout.CouponId.Value 
                        && cc.CustomerId == userId 
                        && !cc.IsUsed);

                if (couponCustomer == null || couponCustomer.Coupon == null)
                {
                    TempData["Error"] = "Mã giảm giá không hợp lệ hoặc đã được sử dụng.";
                    return RedirectToAction("Checkout");
                }

                // Check if coupon is expired or inactive
                if (!couponCustomer.Coupon.IsActive || 
                    (couponCustomer.Coupon.ExpiryDate.HasValue && couponCustomer.Coupon.ExpiryDate.Value.Date < DateTime.Today))
                {
                    TempData["Error"] = "Mã giảm giá này đã hết hạn.";
                    return RedirectToAction("Checkout");
                }

                // Check if user is VIP (only VIPs can use coupons)
                var isVip = await _customerLoyaltyService.IsVipAsync(userId);
                if (!isVip)
                {
                    TempData["Error"] = "Chỉ khách VIP mới có thể sử dụng mã giảm giá.";
                    return RedirectToAction("Checkout");
                }
            }

            var order = await _orderService.CreateOrderAsync(cart, checkout, userId);

            // Mark coupon as used if applied
            if (checkout.CouponId.HasValue && !string.IsNullOrEmpty(userId))
            {
                var couponCustomer = await _context.CouponCustomers
                    .FirstOrDefaultAsync(cc => cc.CouponCustomerId == checkout.CouponId.Value);

                if (couponCustomer != null)
                {
                    couponCustomer.IsUsed = true;
                    couponCustomer.UsedDate = DateTime.Now;
                    _context.CouponCustomers.Update(couponCustomer);
                    await _context.SaveChangesAsync();
                }
            }

            _cartService.ClearCart();

            // After creating order, redirect to Payment selection page
            return RedirectToAction("Payment", new { orderId = order.OrderId });
        }

        // API endpoint to get customer's available coupons
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyCoupons()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var coupons = await _context.CouponCustomers
                .Where(cc => cc.CustomerId == userId && !cc.IsUsed)
                .Include(cc => cc.Coupon)
                .Where(cc => cc.Coupon != null && cc.Coupon.IsActive && 
                    (!cc.Coupon.ExpiryDate.HasValue || cc.Coupon.ExpiryDate.Value.Date >= DateTime.Today))
                .Select(cc => new
                {
                    id = cc.CouponCustomerId,
                    code = cc.Coupon.Code,
                    discountPercent = cc.Coupon.DiscountPercent,
                    customerType = cc.Coupon.CustomerType
                })
                .ToListAsync();

            return Json(coupons);
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
            else if (method == "VNPAY")
            {
                try
                {
                    var fallbackReturnUrl = Url.Action(
                        nameof(VnPayReturn),
                        "Cart",
                        values: null,
                        protocol: Request.Scheme) ?? string.Empty;

                    var paymentUrl = _vnPayService.CreatePaymentUrl(
                        order,
                        HttpContext,
                        fallbackReturnUrl);

                    order.Status = "PendingPayment";
                    await _context.SaveChangesAsync();

                    return Redirect(paymentUrl);
                }
                catch (InvalidOperationException ex)
                {
                    order.Status = "PaymentFailed";
                    await _context.SaveChangesAsync();

                    TempData["Error"] = ex.Message;
                    return RedirectToAction(
                        nameof(Payment),
                        new { orderId = order.OrderId });
                }
            }
            else if (method == "Online" || method == "STRIPE")
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
        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            var callback = _vnPayService.ReadCallback(Request.Query);
            Order? order = null;
            var displayMessage = callback.Message;

            if (callback.OrderId.HasValue)
            {
                order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(
                        o => o.OrderId == callback.OrderId.Value);
            }

            if (!callback.IsValidSignature)
            {
                displayMessage =
                    "Chu ky VNPAY khong hop le. Giao dich khong duoc ghi nhan.";
            }
            else if (order == null)
            {
                displayMessage = "Khong tim thay don hang tu ma giao dich VNPAY.";
            }
            else if (decimal.Round(order.TotalPrice, 0) !=
                     decimal.Round(callback.Amount, 0))
            {
                displayMessage =
                    "So tien VNPAY tra ve khong khop voi tong tien don hang.";
            }
            else if (callback.IsSuccess)
            {
                // IPN la kenh cap nhat chinh. Doan nay xu ly idempotent
                // de moi truong demo van hoat dong neu IPN den cham.
                if (order.Status != "Confirmed")
                {
                    order.Status = "Confirmed";
                    await _context.SaveChangesAsync();

                    await _orderEmailService
                        .SendOrderConfirmationAsync(order.OrderId);
                }

                displayMessage = "Thanh toan VNPAY thanh cong.";
            }
            else if (order.Status == "PendingPayment")
            {
                order.Status = "PaymentFailed";
                await _context.SaveChangesAsync();
            }

            return View(
                "VnPayResult",
                new VnPayResultViewModel
                {
                    Order = order,
                    Result = callback,
                    DisplayMessage = displayMessage
                });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayIpn()
        {
            try
            {
                var callback = _vnPayService.ReadCallback(Request.Query);

                if (!callback.IsValidSignature)
                {
                    return CreateVnPayIpnResponse("97", "Invalid signature");
                }

                if (!callback.OrderId.HasValue)
                {
                    return CreateVnPayIpnResponse("01", "Order not found");
                }

                var order = await _context.Orders
                    .FirstOrDefaultAsync(
                        o => o.OrderId == callback.OrderId.Value);

                if (order == null)
                {
                    return CreateVnPayIpnResponse("01", "Order not found");
                }

                if (decimal.Round(order.TotalPrice, 0) !=
                    decimal.Round(callback.Amount, 0))
                {
                    return CreateVnPayIpnResponse("04", "Invalid amount");
                }

                if (order.Status == "Confirmed")
                {
                    return CreateVnPayIpnResponse("02", "Order already confirmed");
                }

                order.Status = callback.IsSuccess
                    ? "Confirmed"
                    : "PaymentFailed";

                await _context.SaveChangesAsync();

                if (callback.IsSuccess)
                {
                    await _orderEmailService
                        .SendOrderConfirmationAsync(order.OrderId);
                }

                return CreateVnPayIpnResponse("00", "Confirm Success");
            }
            catch (Exception)
            {
                return CreateVnPayIpnResponse("99", "Unknown error");
            }
        }

        private static ContentResult CreateVnPayIpnResponse(
            string responseCode,
            string message)
        {
            var json = JsonSerializer.Serialize(
                new
                {
                    RspCode = responseCode,
                    Message = message
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });

            return new ContentResult
            {
                Content = json,
                ContentType = "application/json; charset=utf-8",
                StatusCode = StatusCodes.Status200OK
            };
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