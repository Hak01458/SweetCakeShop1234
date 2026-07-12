namespace SweetCakeShop.Models
{
    public class Ingredient
    {
        public int IngredientID { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Measurement { get; set; } = string.Empty;
    }
}