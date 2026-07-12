namespace SweetCakeShop.Models
{
    public class AdminProductStockViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public int CanMakeCount { get; set; }

        public bool HasEnoughIngredients { get; set; }

        public List<AdminRecipeIngredientViewModel> Ingredients { get; set; } = new();
    }

    public class AdminRecipeIngredientViewModel
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public decimal InStock { get; set; }          // tồn kho
        public decimal RequiredPerCake { get; set; }  // cần cho 1 bánh
        public string Measurement { get; set; } = string.Empty;
    }
}