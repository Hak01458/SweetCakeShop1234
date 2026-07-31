namespace SweetCakeShop.Models.GHN
{
    public class GhnFeeResponse
    {
        public int Code { get; set; }

        public string Message { get; set; } = "";

        public FeeResponseDto Data { get; set; } = new();
    }
}
