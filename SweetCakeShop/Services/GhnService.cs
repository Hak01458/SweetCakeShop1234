namespace SweetCakeShop.Services;
using SweetCakeShop.Models;
using System.Text.Json;

public class GhnService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    public GhnService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
     
    }

    public async Task<string> GetProvincesAsync()
    {
        var res = await _http.GetAsync("https://provinces.open-api.vn/api/p/");
        var raw = await res.Content.ReadAsStringAsync();
        // API này trả về thẳng array, không cần parse thêm
        return raw;
    }

    public async Task<string> GetDistrictsAsync(int provinceCode)
    {
        // GET /api/p/{code}?depth=2 → trả về tỉnh kèm mảng districts
        var res = await _http.GetAsync($"https://provinces.open-api.vn/api/p/{provinceCode}?depth=2");
        var raw = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(raw);
        return json.GetProperty("districts").GetRawText();
    }

    public async Task<string> GetWardsAsync(int districtCode)
    {
        // GET /api/d/{code}?depth=2 → trả về huyện kèm mảng wards
        var res = await _http.GetAsync($"https://provinces.open-api.vn/api/d/{districtCode}?depth=2");
        var raw = await res.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(raw);
        return json.GetProperty("wards").GetRawText();
    }
    // Tính phí ship trước khi khách đặt
    public async Task<decimal> CalculateFeeAsync(int toDistrictId, string toWardCode)
    {
        var body = new
        {
            from_district_id = int.Parse(_config["GHN:FromDistrictId"]!),
            service_type_id = 2,
            to_district_id = toDistrictId,
            to_ward_code = toWardCode,
            weight = 500,
            insurance_value = 0
        };

        using var ghnHttp = new HttpClient();
        ghnHttp.DefaultRequestHeaders.Add("Token", _config["GHN:ApiToken"]);
        ghnHttp.DefaultRequestHeaders.Add("ShopId", _config["GHN:ShopId"]);

        var res = await ghnHttp.PostAsJsonAsync(
            "https://dev-online-gateway.ghn.vn/shiip/public-api/v2/shipping-order/fee", body);
        var json = await res.Content.ReadFromJsonAsync<GhnFeeResponse>();
        return json?.Data?.Total ?? 0;
    }

    // Tạo đơn vận chuyển sau khi khách thanh toán xong
    public async Task<string?> CreateOrderAsync(Order order)
    {
        var body = new
        {
            shop_id = int.Parse(_config["GHN:ShopId"]!),
            to_name = order.CustomerName,
            to_phone = order.CustomerPhone,
            to_address = order.ShippingAddress,
            to_ward_code = order.Ward,
            to_district_id = int.Parse(order.District ?? "0"),
            weight = 500,
            service_type_id = 2,
            payment_type_id = order.Status == "COD" ? 2 : 1,
            required_note = "KHONGCHOXEMHANG",
            items = order.OrderDetails.Select(d => new {
                name = $"Product {d.ProductId}",
                quantity = d.Quantity,
                price = (int)d.Price
            }).ToList()
        };

        // Tạo HttpClient riêng với GHN token cho việc tạo đơn
        using var ghnHttp = new HttpClient();
        ghnHttp.DefaultRequestHeaders.Add("Token", _config["GHN:ApiToken"]);
        ghnHttp.DefaultRequestHeaders.Add("ShopId", _config["GHN:ShopId"]);

        var res = await ghnHttp.PostAsJsonAsync(
            "https://dev-online-gateway.ghn.vn/shiip/public-api/v2/shipping-order/create", body);
        var json = await res.Content.ReadFromJsonAsync<GhnCreateResponse>();
        return json?.Data?.OrderCode;
    }
    public class GhnFeeResponse
        {
            public int Code { get; set; }
            public GhnFeeData? Data { get; set; }
        }

        public class GhnFeeData
        {
            public decimal Total { get; set; }
        }

        public class GhnCreateResponse
        {
            public int Code { get; set; }
            public GhnCreateData? Data { get; set; }
        }

        public class GhnCreateData
        {
            public string? OrderCode { get; set; }
        }
    }
