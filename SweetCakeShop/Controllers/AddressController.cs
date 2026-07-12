using Microsoft.AspNetCore.Mvc;
using SweetCakeShop.Services;

namespace SweetCakeShop.Controllers
{
    [Route("Address")]
    public class AddressController : Controller
    {
        private readonly AddressService _service;

        public AddressController(AddressService service)
        {
            _service = service;
        }

        [HttpGet("Provinces")]
        public async Task<IActionResult> Provinces()
        {
            return Content(await _service.GetProvinces(),
                "application/json");
        }

        [HttpGet("Districts/{id}")]
        public async Task<IActionResult> Districts(int id)
        {
            return Content(await _service.GetDistricts(id),
                "application/json");
        }

        [HttpGet("Wards/{id}")]
        public async Task<IActionResult> Wards(int id)
        {
            return Content(await _service.GetWards(id),
                "application/json");
        }
    }
}
