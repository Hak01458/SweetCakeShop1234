using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class OrdersModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdersModel(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public List<Order> Orders { get; set; } = new List<Order>();
        public List<ProductReview> Reviews { get; set; } = new();
        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Orders = new List<Order>();
                return;
            }

            Orders = await _db.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            Reviews = await _db.ProductReviews
            .Where(r => r.UserId == user.Id)
            .ToListAsync();

        }   
        public async Task<IActionResult> OnPostCancelAsync(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToPage();

            var order = await _db.Orders
                .FirstOrDefaultAsync(o =>
                    o.OrderId == orderId &&
                    o.UserId == user.Id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToPage();
            }

            // Chỉ cho hủy khi Pending hoặc Confirmed
            if (order.Status != "COD" &&
                order.Status != "Confirmed")
            {
                TempData["Error"] = "Đơn hàng không thể hủy.";
                return RedirectToPage();
            }

            order.Status = "Cancelled";

            await _db.SaveChangesAsync();

            TempData["Success"] = "Hủy đơn hàng thành công.";

            return RedirectToPage();
        }
    }
}
