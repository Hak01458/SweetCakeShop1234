using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;

namespace SweetCakeShop.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class MessagesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public MessagesModel(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public List<ContactMessage> Messages { get; set; } = new();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Messages = new List<ContactMessage>();
                return;
            }

            Messages = await _db.ContactMessages
                .Where(m => m.UserId == user.Id)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            // Đánh dấu đã xem các phản hồi mới khi khách mở trang này
            var unseenReplies = Messages.Where(m => !m.IsReadByCustomer).ToList();
            if (unseenReplies.Count > 0)
            {
                foreach (var m in unseenReplies) m.IsReadByCustomer = true;
                await _db.SaveChangesAsync();
            }
        }
    }
}