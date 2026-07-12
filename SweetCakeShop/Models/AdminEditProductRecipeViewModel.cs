namespace SweetCakeShop.Models
{
    public class AdminEditProductRecipeViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public List<AdminProductRecipeItemViewModel> RecipeItems { get; set; } = new();
        public List<IngredientOptionViewModel> IngredientOptions { get; set; } = new();
    }

    public class AdminProductRecipeItemViewModel
    {
        public int RecipeId { get; set; }
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Measurement { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }

    public class IngredientOptionViewModel
    {
        public int IngredientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Measurement { get; set; } = string.Empty;
    }
}