namespace SweetCakeShop.Models
{
    public class Recipe
    {
        public int RecipeID { get; set; }

        public int ProductID { get; set; }
        public int IngredientsID { get; set; }
        public decimal Quantity { get; set; }

        public Product? Product { get; set; }
        public Ingredient? Ingredient { get; set; }
    }
}