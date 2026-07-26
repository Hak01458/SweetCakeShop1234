using System.Text.Json.Serialization;

namespace SweetCakeShop.Models.GHN
{
    public class CreateOrderResponseDto
    {
        [JsonPropertyName("order_code")]
        public string? OrderCode { get; set; }

        [JsonPropertyName("sort_code")]
        public string? SortCode { get; set; }
    }
}