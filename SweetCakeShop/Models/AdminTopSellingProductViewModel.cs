namespace SweetCakeShop.Models
{
    public class AdminTopSellingProductViewModel
    {
        public int Rank { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SoldQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ConfirmedOrderCount { get; set; }
    }
}