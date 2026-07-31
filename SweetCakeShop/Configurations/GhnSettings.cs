namespace SweetCakeShop.Configurations
{
    public class GhnSettings
    {
        public string BaseUrl { get; set; } = "";

        public string Token { get; set; } = "";

        public int ShopId { get; set; }

        public int FromDistrictId { get; set; }

        public string FromWardCode { get; set; } = "";

        public int DefaultWeight { get; set; } = 500;

        public int DefaultLength { get; set; } = 20;

        public int DefaultWidth { get; set; } = 20;

        public int DefaultHeight { get; set; } = 10;
    }
}
