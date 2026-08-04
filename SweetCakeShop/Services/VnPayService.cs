using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SweetCakeShop.Configurations;
using SweetCakeShop.Models;

namespace SweetCakeShop.Services
{
    public sealed class VnPayService : IVnPayService
    {
        private readonly VnPaySettings _settings;

        public VnPayService(IOptions<VnPaySettings> settings)
        {
            _settings = settings.Value;
        }

        public string CreatePaymentUrl(
            Order order,
            HttpContext httpContext,
            string fallbackReturnUrl,
            string? bankCode = null)
        {
            ValidateSettings();

            if (order.TotalPrice <= 0)
            {
                throw new InvalidOperationException(
                    "So tien thanh toan VNPAY phai lon hon 0.");
            }

            var now = GetVietnamTime();
            var returnUrl = string.IsNullOrWhiteSpace(_settings.ReturnUrl)
                ? fallbackReturnUrl
                : _settings.ReturnUrl;

            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                throw new InvalidOperationException(
                    "Khong tao duoc VNPAY ReturnUrl.");
            }

            // Alphanumeric va khong trung lap trong ngay.
            // Vi du: SC14T20260803141030
            var transactionReference =
                $"SC{order.OrderId}T{now:yyyyMMddHHmmss}";

            var amount = decimal.ToInt64(
                decimal.Round(
                    order.TotalPrice * 100m,
                    0,
                    MidpointRounding.AwayFromZero));

            var parameters = new SortedDictionary<string, string>(
                StringComparer.Ordinal)
            {
                ["vnp_Version"] = _settings.Version,
                ["vnp_Command"] = _settings.Command,
                ["vnp_TmnCode"] = _settings.TmnCode,
                ["vnp_Amount"] = amount.ToString(CultureInfo.InvariantCulture),
                ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = _settings.CurrCode,
                ["vnp_IpAddr"] = GetClientIpAddress(httpContext),
                ["vnp_Locale"] = _settings.Locale,
                ["vnp_OrderInfo"] = $"Thanh toan don hang {order.OrderId}",
                ["vnp_OrderType"] = _settings.OrderType,
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = transactionReference,
                ["vnp_ExpireDate"] = now
                    .AddMinutes(Math.Max(1, _settings.ExpireMinutes))
                    .ToString("yyyyMMddHHmmss")
            };

            if (!string.IsNullOrWhiteSpace(bankCode))
            {
                parameters["vnp_BankCode"] = bankCode.Trim();
            }

            var signData = BuildQueryString(parameters);
            var secureHash = HmacSha512(
                _settings.HashSecret,
                signData);

            return $"{_settings.PaymentUrl.TrimEnd('?')}" +
                   $"?{signData}&vnp_SecureHash={secureHash}";
        }

        public VnPayCallbackResult ReadCallback(IQueryCollection query)
        {
            var parameters = new SortedDictionary<string, string>(
                StringComparer.Ordinal);

            foreach (var item in query)
            {
                if (!item.Key.StartsWith("vnp_", StringComparison.Ordinal))
                {
                    continue;
                }

                if (item.Key.Equals(
                        "vnp_SecureHash",
                        StringComparison.Ordinal) ||
                    item.Key.Equals(
                        "vnp_SecureHashType",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var value = item.Value.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    parameters[item.Key] = value;
                }
            }

            var receivedHash = query["vnp_SecureHash"].ToString();
            var signData = BuildQueryString(parameters);
            var expectedHash = HmacSha512(
                _settings.HashSecret,
                signData);

            var validSignature = FixedTimeEqualsHex(
                expectedHash,
                receivedHash);

            var transactionReference = GetValue(
                parameters,
                "vnp_TxnRef");

            var responseCode = GetValue(
                parameters,
                "vnp_ResponseCode");

            var transactionStatus = GetValue(
                parameters,
                "vnp_TransactionStatus");

            var amount = ParseAmount(
                GetValue(parameters, "vnp_Amount"));

            return new VnPayCallbackResult
            {
                IsValidSignature = validSignature,
                IsSuccess = validSignature &&
                            responseCode == "00" &&
                            transactionStatus == "00",
                OrderId = ParseOrderId(transactionReference),
                Amount = amount,
                TransactionReference = transactionReference,
                TransactionNumber = GetValue(
                    parameters,
                    "vnp_TransactionNo"),
                ResponseCode = responseCode,
                TransactionStatus = transactionStatus,
                BankCode = GetValue(parameters, "vnp_BankCode"),
                PayDate = GetValue(parameters, "vnp_PayDate"),
                Message = GetResponseMessage(responseCode)
            };
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.TmnCode))
            {
                throw new InvalidOperationException(
                    "Chua cau hinh VnPay:TmnCode trong appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(_settings.HashSecret))
            {
                throw new InvalidOperationException(
                    "Chua cau hinh VnPay:HashSecret trong appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(_settings.PaymentUrl))
            {
                throw new InvalidOperationException(
                    "Chua cau hinh VnPay:PaymentUrl trong appsettings.json.");
            }
        }

        private static string BuildQueryString(
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            return string.Join(
                "&",
                parameters
                    .Where(item => !string.IsNullOrEmpty(item.Value))
                    .Select(item =>
                        $"{WebUtility.UrlEncode(item.Key)}=" +
                        $"{WebUtility.UrlEncode(item.Value)}"));
        }

        private static string HmacSha512(
            string key,
            string input)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(input);

            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private static bool FixedTimeEqualsHex(
            string expected,
            string actual)
        {
            try
            {
                var expectedBytes = Convert.FromHexString(expected);
                var actualBytes = Convert.FromHexString(actual);

                return expectedBytes.Length == actualBytes.Length &&
                       CryptographicOperations.FixedTimeEquals(
                           expectedBytes,
                           actualBytes);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static decimal ParseAmount(string rawAmount)
        {
            if (!long.TryParse(
                    rawAmount,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var amount))
            {
                return 0m;
            }

            return amount / 100m;
        }

        private static int? ParseOrderId(string transactionReference)
        {
            // Dinh dang: SC{OrderId}T{yyyyMMddHHmmss}
            if (string.IsNullOrWhiteSpace(transactionReference) ||
                !transactionReference.StartsWith(
                    "SC",
                    StringComparison.Ordinal))
            {
                return null;
            }

            var separatorIndex = transactionReference.IndexOf(
                'T',
                2);

            if (separatorIndex <= 2)
            {
                return null;
            }

            var rawOrderId = transactionReference.Substring(
                2,
                separatorIndex - 2);

            return int.TryParse(
                rawOrderId,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var orderId)
                ? orderId
                : null;
        }

        private static string GetValue(
            IReadOnlyDictionary<string, string> parameters,
            string key)
        {
            return parameters.TryGetValue(key, out var value)
                ? value
                : string.Empty;
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            var cloudflareIp = context.Request.Headers[
                "CF-Connecting-IP"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(cloudflareIp))
            {
                return cloudflareIp;
            }

            var forwardedFor = context.Request.Headers[
                "X-Forwarded-For"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp == null)
            {
                return "127.0.0.1";
            }

            if (remoteIp.IsIPv4MappedToIPv6)
            {
                remoteIp = remoteIp.MapToIPv4();
            }

            return remoteIp.ToString() == "::1"
                ? "127.0.0.1"
                : remoteIp.ToString();
        }

        private static DateTime GetVietnamTime()
        {
            var utcNow = DateTime.UtcNow;

            foreach (var id in new[]
                     {
                         "SE Asia Standard Time",
                         "Asia/Ho_Chi_Minh"
                     })
            {
                try
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
                    return TimeZoneInfo.ConvertTimeFromUtc(
                        utcNow,
                        timeZone);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return utcNow.AddHours(7);
        }

        private static string GetResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dich thanh cong.",
                "07" => "Giao dich bi nghi ngo gian lan.",
                "09" => "Tai khoan chua dang ky Internet Banking.",
                "10" => "Xac thuc thong tin the khong dung qua so lan quy dinh.",
                "11" => "Giao dich da het han thanh toan.",
                "12" => "Tai khoan hoac the dang bi khoa.",
                "13" => "Ma OTP khong dung.",
                "24" => "Khach hang da huy giao dich.",
                "51" => "Tai khoan khong du so du.",
                "65" => "Tai khoan da vuot han muc giao dich trong ngay.",
                "75" => "Ngan hang dang bao tri.",
                "79" => "Nhap sai mat khau thanh toan qua so lan quy dinh.",
                "99" => "Giao dich khong thanh cong.",
                _ => string.IsNullOrWhiteSpace(responseCode)
                    ? "Khong nhan duoc ma ket qua tu VNPAY."
                    : $"Giao dich khong thanh cong. Ma loi: {responseCode}."
            };
        }
    }
}
