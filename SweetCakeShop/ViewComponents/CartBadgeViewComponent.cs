using Microsoft.AspNetCore.Mvc;
using SweetCakeShop.Services;

namespace SweetCakeShop.ViewComponents
{
    public class CartBadgeViewComponent : ViewComponent
    {
        private readonly CartService _cartService;

        public CartBadgeViewComponent(CartService cartService)
        {
            _cartService = cartService;
        }

        public IViewComponentResult Invoke()
        {
            var count = _cartService.GetCart().Items.Sum(i => i.Quantity);
            return View("Default", count);
        }
    }
}
