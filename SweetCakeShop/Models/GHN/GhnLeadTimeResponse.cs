namespace SweetCakeShop.Models.GHN
{
    public class GhnLeadTimeResponse
    {
        public int Code { get; set; }

        public string Message { get; set; } = "";

        public LeadTimeDto Data { get; set; } = new();
    }
}
