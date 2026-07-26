using System.Text.Json.Serialization;

namespace SweetCakeShop.Models.GHN
{
    public class GhnCreateResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public CreateOrderResponseDto Data { get; set; } = new();
    }
}