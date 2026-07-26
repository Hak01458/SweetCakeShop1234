namespace SweetCakeShop.Services;
using SweetCakeShop.Models;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SweetCakeShop.Configurations;
using SweetCakeShop.Models.GHN;
using System.Text.Json.Serialization;
public class GhnService
{
    private readonly HttpClient _http;
    private readonly GhnSettings _settings;

    public GhnService(HttpClient http, IOptions<GhnSettings> options)
    {
        _http = http;
        _settings = options.Value;

        _http.BaseAddress = new Uri(_settings.BaseUrl);

        if (!_http.DefaultRequestHeaders.Contains("Token"))
            _http.DefaultRequestHeaders.Add("Token", _settings.Token);

        if (!_http.DefaultRequestHeaders.Contains("ShopId"))
            _http.DefaultRequestHeaders.Add("ShopId", _settings.ShopId.ToString());
    }

    public async Task<List<ProvinceDto>> GetProvincesAsync()
    {
        var response = await _http.GetAsync("/shiip/public-api/master-data/province");

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        var list = new List<ProvinceDto>();

        foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            list.Add(new ProvinceDto
            {
                ProvinceID = item.GetProperty("ProvinceID").GetInt32(),
                ProvinceName = item.GetProperty("ProvinceName").GetString()!,
                RegionID = item.GetProperty("RegionID").GetInt32()
            });
        }

        Console.WriteLine($"Province Count = {list.Count}");

        return list;
    }

    public async Task<List<DistrictDto>> GetDistrictsAsync(int provinceId)
    {
        var response = await _http.PostAsJsonAsync(
            "/shiip/public-api/master-data/district",
            new
            {
                province_id = provinceId
            });

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<GhnResponse<DistrictDto>>();

        return result?.Data ?? new List<DistrictDto>();
    }

    public async Task<List<WardDto>> GetWardsAsync(int districtId)
    {
        var response = await _http.PostAsJsonAsync(
            "/shiip/public-api/master-data/ward",
            new
            {
                district_id = districtId
            });

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<GhnResponse<WardDto>>();

        return result?.Data ?? new List<WardDto>();
    }

    public async Task<int> GetAvailableServiceAsync(int toDistrictId)
    {
        var body = new
        {
            shop_id = _settings.ShopId,
            from_district = _settings.FromDistrictId,
            to_district = toDistrictId
        };

        var response = await _http.PostAsJsonAsync(
            "/shiip/public-api/v2/shipping-order/available-services",
            body);

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<GhnResponse<ServiceDto>>();

        var service = result?.Data.FirstOrDefault();

        if (service == null)
            throw new Exception("GHN không trả về dịch vụ vận chuyển.");

        return service.service_id;
    }

    // Tính phí ship trước khi khách đặt
    public async Task<decimal> CalculateFeeAsync(int toDistrictId, string toWardCode)
    {   
        // Lấy service_id phù hợp
        var serviceId = await GetAvailableServiceAsync(toDistrictId);

        var body = new
        {
            from_district_id = _settings.FromDistrictId,
            to_district_id = toDistrictId,
            to_ward_code = toWardCode,

            service_id = serviceId,

            weight = _settings.DefaultWeight,
            length = _settings.DefaultLength,
            width = _settings.DefaultWidth,
            height = _settings.DefaultHeight,

            insurance_value = 0
        };

        var response = await _http.PostAsJsonAsync(
    "/shiip/public-api/v2/shipping-order/fee",
    body);

        var json = await response.Content.ReadAsStringAsync();

        Console.WriteLine("========== GHN Fee ==========");
        Console.WriteLine((int)response.StatusCode);
        Console.WriteLine(json);

        response.EnsureSuccessStatusCode();

        var result = JsonSerializer.Deserialize<GhnFeeResponse>(
     json,
     new JsonSerializerOptions
     {
         PropertyNameCaseInsensitive = true
     });

        if (result?.Data == null)
            throw new Exception("Không lấy được phí vận chuyển từ GHN.");

        return result.Data.Total;
    }

    public async Task<DateTime?> GetLeadTimeAsync(int toDistrictId, string toWardCode)
    {
        // Lấy service_id phù hợp
        var serviceId = await GetAvailableServiceAsync(toDistrictId);

        var body = new
        {
            from_district_id = _settings.FromDistrictId,
            from_ward_code = _settings.FromWardCode,

            to_district_id = toDistrictId,
            to_ward_code = toWardCode,

            service_id = serviceId
        };

        var response = await _http.PostAsJsonAsync(
            "/shiip/public-api/v2/shipping-order/leadtime",
            body);

        var json = await response.Content.ReadAsStringAsync();

        Console.WriteLine("========== GHN LeadTime ==========");
        Console.WriteLine((int)response.StatusCode);
        Console.WriteLine(json);

        response.EnsureSuccessStatusCode();

        var result = JsonSerializer.Deserialize<GhnLeadTimeResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result?.Data == null)
            throw new Exception("Không lấy được thời gian giao hàng từ GHN.");

        return DateTimeOffset
            .FromUnixTimeSeconds(result.Data.LeadTime)
            .ToOffset(TimeSpan.FromHours(7))
            .DateTime;
    }

    // Tạo đơn vận chuyển sau khi khách thanh toán xong

    public async Task<string?> CreateOrderAsync(Order order)
    {
        var body = new
        {
            shop_id = _settings.ShopId,
            to_name = order.CustomerName,
            to_phone = order.CustomerPhone,
            to_address = order.ShippingAddress,
            to_ward_code = order.WardCode,
            to_district_id = order.DistrictId ?? 0,
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

        Console.WriteLine("===== CREATE BODY =====");
        Console.WriteLine(JsonSerializer.Serialize(body));
        var res = await _http.PostAsJsonAsync(
     "/shiip/public-api/v2/shipping-order/create",
     body);

        var jsonString = await res.Content.ReadAsStringAsync();

        Console.WriteLine(jsonString);

        res.EnsureSuccessStatusCode();

        var result = JsonSerializer.Deserialize<GhnCreateResponse>(
            jsonString,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return result?.Data?.OrderCode;
    }

    public async Task CreateShippingOrderAsync(Order order)
    {
        if (!string.IsNullOrEmpty(order.GhnOrderCode))
            return;

        try
        {
            var orderCode = await CreateOrderAsync(order);
            Console.WriteLine("OrderCode nhận được: " + orderCode);

            var leadTime = await GetLeadTimeAsync(
                order.DistrictId!.Value,
                order.WardCode!);

            order.GhnOrderCode = orderCode;
            order.ExpectedDeliveryDate = leadTime;
            order.TrackingUrl = $"https://donhang.ghn.vn/?order_code={orderCode}";
            order.ShippingStatus = "ready_to_pick";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GHN Error: {ex.Message}");
            order.ShippingStatus = "GHN_ERROR";
        }
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
            [JsonPropertyName("code")]
            public int Code { get; set; }

            [JsonPropertyName("data")]
            public GhnCreateData? Data { get; set; }
        }

    public class GhnCreateData
        {
            [JsonPropertyName("order_code")]
            public string? OrderCode { get; set; }
        }
}
