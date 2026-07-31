namespace SweetCakeShop.Models
{
    public class Ingredient
    {
        public int IngredientID { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Measurement { get; set; } = string.Empty;
        public DateTime? ImportDate { get; set; }   // Ngày nhập
        public DateTime? ExpiryDate { get; set; }
    }
}