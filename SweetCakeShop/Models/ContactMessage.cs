using Microsoft.AspNetCore.Identity;

namespace SweetCakeShop.Models
{
    public class ContactMessage
    {
        public int ContactMessageId { get; set; }

        /// <summary>
        /// Null nếu khách gửi tin nhắn lúc chưa đăng nhập (khách vãng lai).
        /// Có giá trị nếu khách đã đăng nhập -> dùng để hiển thị trong "Tin nhắn của tôi".
        /// </summary>
        public string? UserId { get; set; }
        public IdentityUser? User { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Phản hồi từ Admin
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }

        /// <summary>Admin đã đọc tin nhắn của khách chưa (để hiện badge "mới" bên Admin).</summary>
        public bool IsReadByAdmin { get; set; } = false;

        /// <summary>Khách đã xem phản hồi của Admin chưa (để hiện badge "mới" bên trang khách).</summary>
        public bool IsReadByCustomer { get; set; } = true; // true khi chưa có reply, sẽ set false ngay khi Admin phản hồi
    }
}