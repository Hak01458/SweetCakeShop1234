namespace SweetCakeShop.Configurations
{
    public class EmailSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";

        public int Port { get; set; } = 587;

        public string SenderName { get; set; } = "Sweet Cake Shop";

        public string SenderEmail { get; set; } = string.Empty;

        public string SenderPassword { get; set; } = string.Empty;
    }
}