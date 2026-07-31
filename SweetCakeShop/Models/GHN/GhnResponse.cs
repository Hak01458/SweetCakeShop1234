namespace SweetCakeShop.Models.GHN
{
    public class GhnResponse<T>
    {
        public int Code { get; set; }

        public string Message { get; set; } = "";

        public List<T> Data { get; set; } = new();
    }
}
