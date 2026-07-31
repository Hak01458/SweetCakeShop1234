using Microsoft.AspNetCore.Mvc;
using SweetCakeShop.Services;

namespace SweetCakeShop.Controllers
{
    [Route("Address")]
    public class AddressController : Controller
    {
        private readonly GhnService _ghnService;

        public AddressController(GhnService ghnService)
        {
            _ghnService = ghnService;
        }

        [HttpGet("Provinces")]
        public async Task<IActionResult> Provinces()
        {
            var provinces = await _ghnService.GetProvincesAsync();
            return Ok(provinces);
        }

        [HttpGet("Districts/{provinceId}")]
        public async Task<IActionResult> Districts(int provinceId)
        {
            var districts = await _ghnService.GetDistrictsAsync(provinceId);
            return Ok(districts);
        }

        [HttpGet("Wards/{districtId}")]
        public async Task<IActionResult> Wards(int districtId)
        {
            var wards = await _ghnService.GetWardsAsync(districtId);
            return Ok(wards);
        }
    }

    public class ProvinceRequest
    {
        public int ProvinceId { get; set; }
    }

    public class DistrictRequest
    {
        public int DistrictId { get; set; }
    }
}