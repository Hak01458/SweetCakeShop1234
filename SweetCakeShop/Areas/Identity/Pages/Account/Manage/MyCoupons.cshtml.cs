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
    public class MyCouponsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MyCouponsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<CouponCustomer> CouponCustomers { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            CouponCustomers = await _context.CouponCustomers
                .Where(cc => cc.CustomerId == user.Id)
                .Include(cc => cc.Coupon)
                .OrderByDescending(cc => cc.AssignedDate)
                .ToListAsync();

            return Page();
        }
    }
}
