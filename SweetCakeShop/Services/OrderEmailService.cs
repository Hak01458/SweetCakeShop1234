using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;

namespace SweetCakeShop.Services
{
    public sealed class OrderEmailService
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<OrderEmailService> _logger;

        public OrderEmailService(
            ApplicationDbContext db,
            IEmailSender emailSender,
            ILogger<OrderEmailService> logger)
        {
            _db = db;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task SendOrderConfirmationAsync(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(order.CustomerEmail))
            {
                return;
            }

            // Đã gửi rồi thì không gửi lại
            if (order.ConfirmationEmailSentAt.HasValue)
            {
                return;
            }

            static string Encode(string? value)
            {
                return WebUtility.HtmlEncode(value ?? string.Empty);
            }

            static string Money(decimal value)
            {
                return value.ToString(
                    "N0",
                    CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
            }

            var productRows = string.Join(
                string.Empty,
                order.OrderDetails.Select(detail =>
                {
                    var productName = Encode(
                        detail.Product?.ProductName
                        ?? $"Sản phẩm #{detail.ProductId}");

                    var lineTotal =
                        detail.Price * detail.Quantity;

                    return $"""
                    <tr>
                        <td style="padding:10px;border-bottom:1px solid #eee;">
                            {productName}
                        </td>

                        <td style="padding:10px;border-bottom:1px solid #eee;
                                   text-align:center;">
                            {detail.Quantity}
                        </td>

                        <td style="padding:10px;border-bottom:1px solid #eee;
                                   text-align:right;">
                            {Money(detail.Price)}
                        </td>

                        <td style="padding:10px;border-bottom:1px solid #eee;
                                   text-align:right;">
                            {Money(lineTotal)}
                        </td>
                    </tr>
                    """;
                }));

            var fullAddress = string.Join(
                ", ",
                new[]
                {
                    order.ShippingAddress,
                    order.Ward,
                    order.District,
                    order.Province
                }
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value)));

            var htmlMessage = $"""
            <!DOCTYPE html>
            <html lang="vi">
            <body style="font-family:Arial,sans-serif;
                         background:#fff7fa;
                         padding:30px;">

                <div style="max-width:720px;
                            margin:auto;
                            background:white;
                            border-radius:18px;
                            padding:30px;
                            box-shadow:0 6px 20px rgba(0,0,0,0.08);">

                    <h2 style="color:#d81b60;text-align:center;">
                        Đặt hàng thành công
                    </h2>

                    <p>
                        Xin chào
                        <strong>{Encode(order.CustomerName)}</strong>,
                    </p>

                    <p>
                        SweetCakeShop đã nhận được đơn hàng của bạn.
                    </p>

                    <div style="background:#fff0f5;
                                border-radius:12px;
                                padding:15px;
                                margin:20px 0;">

                        <p>
                            <strong>Mã đơn hàng:</strong>
                            #{order.OrderId}
                        </p>

                        <p>
                            <strong>Ngày đặt:</strong>
                            {order.OrderDate:dd/MM/yyyy HH:mm}
                        </p>

                        <p>
                            <strong>Trạng thái:</strong>
                            {Encode(order.Status)}
                        </p>

                        <p>
                            <strong>Số điện thoại:</strong>
                            {Encode(order.CustomerPhone)}
                        </p>

                        <p>
                            <strong>Địa chỉ giao hàng:</strong>
                            {Encode(fullAddress)}
                        </p>
                    </div>

                    <table style="width:100%;
                                  border-collapse:collapse;
                                  margin-top:20px;">

                        <thead>
                            <tr style="background:#d81b60;color:white;">
                                <th style="padding:10px;text-align:left;">
                                    Sản phẩm
                                </th>

                                <th style="padding:10px;">
                                    Số lượng
                                </th>

                                <th style="padding:10px;text-align:right;">
                                    Đơn giá
                                </th>

                                <th style="padding:10px;text-align:right;">
                                    Thành tiền
                                </th>
                            </tr>
                        </thead>

                        <tbody>
                            {productRows}
                        </tbody>
                    </table>

                    <div style="text-align:right;margin-top:20px;">
                        <p>
                            Phí vận chuyển:
                            <strong>{Money(order.ShippingFee)}</strong>
                        </p>

                        <h3 style="color:#d81b60;">
                            Tổng thanh toán:
                            {Money(order.TotalPrice)}
                        </h3>
                    </div>

                    <p style="margin-top:30px;">
                        Cảm ơn bạn đã mua hàng tại SweetCakeShop.
                    </p>

                    <p style="color:#777;font-size:14px;">
                        Email này được gửi tự động,
                        vui lòng không trả lời.
                    </p>
                </div>
            </body>
            </html>
            """;

            try
            {
                await _emailSender.SendEmailAsync(
                    order.CustomerEmail,
                    $"Xác nhận đơn hàng #{order.OrderId} - SweetCakeShop",
                    htmlMessage);

                order.ConfirmationEmailSentAt =
                    DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Gửi mail lỗi không được làm mất đơn hàng
                _logger.LogError(
                    ex,
                    "Gửi email đơn hàng #{OrderId} thất bại.",
                    order.OrderId);
            }
        }
    }
}