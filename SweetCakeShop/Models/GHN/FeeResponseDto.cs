using System.Text.Json.Serialization;

namespace SweetCakeShop.Models.GHN
{
    public class FeeResponseDto
    {
        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("service_fee")]
        public decimal ServiceFee { get; set; }

        [JsonPropertyName("insurance_fee")]
        public decimal InsuranceFee { get; set; }

        [JsonPropertyName("pick_station_fee")]
        public decimal PickStationFee { get; set; }

        [JsonPropertyName("coupon_value")]
        public decimal CouponValue { get; set; }
    }
}