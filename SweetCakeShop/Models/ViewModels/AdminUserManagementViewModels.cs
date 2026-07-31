using System.ComponentModel.DataAnnotations;

namespace SweetCakeShop.Models.ViewModels
{
    public sealed class AdminUserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public int OrderCount { get; set; }
        public int DeliveredOrderCount { get; set; }
        public decimal TotalSpent { get; set; }

        public bool IsLocked =>
            LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;

        public bool IsVip => DeliveredOrderCount >= 5;
    }

    public sealed class AdminUserIndexViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Role { get; set; }
        public IList<AdminUserListItemViewModel> Users { get; set; }
            = new List<AdminUserListItemViewModel>();
    }

    public sealed class AdminUserDetailsViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public IList<Order> Orders { get; set; } = new List<Order>();
        public int TotalOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalSpent { get; set; }

        public bool IsVip => DeliveredOrders >= 5;

        public bool IsLocked =>
            LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
    }

    public sealed class AdminEditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Email đã xác nhận")]
        public bool EmailConfirmed { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; } = "User";
    }
}
