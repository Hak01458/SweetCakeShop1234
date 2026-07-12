namespace SweetCakeShop.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }          // lưu đường dẫn ảnh, ví dụ: /images/cake1.jpg
        public int CategoryId { get; set; }

        public Category? Category { get; set; }
    }
}
