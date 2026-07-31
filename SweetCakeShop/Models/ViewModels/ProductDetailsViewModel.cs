using System.Collections.Generic;

namespace SweetCakeShop.Models.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> SimilarProducts { get; set; } = new();

        public List<ProductReview> Reviews { get; set; } = new();

        public double AverageRating { get; set; }

        public int TotalReviews { get; set; }
    }
}
