using SweetCakeShop.Models;

namespace SweetCakeShop.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> FeaturedProducts { get; set; } = new();
    }
}
