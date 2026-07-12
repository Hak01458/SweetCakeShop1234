using System.Collections.Generic;

namespace SweetCakeShop.Models.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> SimilarProducts { get; set; } = new();
    }
}
