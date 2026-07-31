using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReviewController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET
        public async Task<IActionResult> Create(int productId, int orderId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // Kiểm tra đơn hàng
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == orderId &&
                    o.UserId == user.Id);

            if (order == null)
                return NotFound();

            // Chỉ được đánh giá khi đã giao
            if (order.Status != "Delivered")
            {
                TempData["Error"] = "Chỉ có thể đánh giá khi đơn hàng đã giao.";
                return RedirectToPage("/Account/Manage/Orders", new { area = "Identity" });
            }

            // Kiểm tra sản phẩm có thuộc đơn hàng
            bool purchased = order.OrderDetails
                .Any(d => d.ProductId == productId);

            if (!purchased)
            {
                TempData["Error"] = "Sản phẩm không thuộc đơn hàng.";
                return RedirectToPage("/Account/Manage/Orders", new { area = "Identity" });
            }

            // Kiểm tra đã đánh giá chưa
            bool reviewed = await _context.ProductReviews.AnyAsync(r =>
                r.ProductId == productId &&
                r.OrderId == orderId &&
                r.UserId == user.Id);

            if (reviewed)
            {
                TempData["Error"] = "Bạn đã đánh giá sản phẩm này.";
                return RedirectToPage("/Account/Manage/Orders", new { area = "Identity" });
            }

            var review = new ProductReview
            {
                ProductId = productId,
                OrderId = orderId
            };

            return View(review);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductReview review)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // Kiểm tra đơn hàng
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == review.OrderId &&
                    o.UserId == user.Id);

            if (order == null)
                return NotFound();

            // Chỉ được đánh giá khi đã giao
            if (order.Status != "Delivered")
            {
                TempData["Error"] = "Chỉ có thể đánh giá sau khi nhận hàng.";
                return RedirectToPage("/Account/Manage/Orders", new { area = "Identity" });
            }

            // Kiểm tra sản phẩm có trong đơn
            bool purchased = order.OrderDetails
                .Any(d => d.ProductId == review.ProductId);

            if (!purchased)
            {
                TempData["Error"] = "Sản phẩm không thuộc đơn hàng.";
                return RedirectToPage("/Account/Manage/Orders", new { area = "Identity" });
            }

            // Không cho đánh giá 2 lần
            bool exists = await _context.ProductReviews.AnyAsync(r =>
                r.ProductId == review.ProductId &&
                r.OrderId == review.OrderId &&
                r.UserId == user.Id);

            if (exists)
            {
                TempData["Error"] = "Bạn đã đánh giá sản phẩm này.";
                return RedirectToPage("/Account/Manage/Orders", new { area = "Identity" });
            }

            review.UserId = user.Id;
            review.CreatedAt = DateTime.Now;

            _context.ProductReviews.Add(review);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";

            return RedirectToPage("/Account/Manage/Orders", new { area = "Identity" });
        }
    }
}