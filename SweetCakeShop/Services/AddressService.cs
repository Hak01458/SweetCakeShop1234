namespace SweetCakeShop.Services
{
    public class AddressService
    {
        private readonly HttpClient _http;

        public AddressService(HttpClient http)
        {
            _http = http;
            _http.BaseAddress = new Uri("https://provinces.open-api.vn/api/");
        }

        public async Task<string> GetProvinces()
        {
            return await _http.GetStringAsync("p/");
        }

        public async Task<string> GetDistricts(int provinceCode)
        {
            return await _http.GetStringAsync($"p/{provinceCode}?depth=2");
        }

        public async Task<string> GetWards(int districtCode)
        {
            return await _http.GetStringAsync($"d/{districtCode}?depth=2");
        }
    }
}
