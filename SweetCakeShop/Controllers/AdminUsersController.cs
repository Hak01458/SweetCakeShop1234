using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;

namespace SweetCakeShop.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public sealed class AdminUsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUsersController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? role)
        {
            await EnsureDefaultRolesAsync();

            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();

                query = query.Where(user =>
                    (user.Email != null && user.Email.Contains(keyword)) ||
                    (user.UserName != null && user.UserName.Contains(keyword)) ||
                    (user.PhoneNumber != null && user.PhoneNumber.Contains(keyword)));
            }

            var users = await query
                .OrderBy(user => user.Email)
                .ToListAsync();

            var userIds = users.Select(user => user.Id).ToList();

            var orderStats = userIds.Count == 0
                ? new Dictionary<string, UserOrderStats>()
                : await _context.Orders
                    .AsNoTracking()
                    .Where(order => userIds.Contains(order.UserId))
                    .GroupBy(order => order.UserId)
                    .Select(group => new UserOrderStats
                    {
                        UserId = group.Key,
                        OrderCount = group.Count(),
                        DeliveredOrderCount = group.Count(order =>
                            order.Status == "Delivered" ||
                            order.Status == "delivered" ||
                            order.ShippingStatus == "delivered"),
                        TotalSpent = group
                            .Where(order =>
                                order.Status == "Delivered" ||
                                order.Status == "delivered" ||
                                order.ShippingStatus == "delivered")
                            .Sum(order => (decimal?)order.TotalPrice) ?? 0m
                    })
                    .ToDictionaryAsync(item => item.UserId);

            var items = new List<AdminUserListItemViewModel>();

            foreach (var user in users)
            {
                var assignedRoles = await _userManager.GetRolesAsync(user);
                var displayRoles = assignedRoles.Count == 0
                    ? new List<string> { nameof(Roles.User) }
                    : assignedRoles.ToList();

                if (!string.IsNullOrWhiteSpace(role) &&
                    !displayRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                orderStats.TryGetValue(user.Id, out var stats);

                items.Add(new AdminUserListItemViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnd = user.LockoutEnd,
                    Roles = displayRoles,
                    OrderCount = stats?.OrderCount ?? 0,
                    DeliveredOrderCount = stats?.DeliveredOrderCount ?? 0,
                    TotalSpent = stats?.TotalSpent ?? 0m
                });
            }

            var model = new AdminUserIndexViewModel
            {
                SearchTerm = searchTerm,
                Role = role,
                Users = items
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var displayRoles = roles.Count == 0
                ? new List<string> { nameof(Roles.User) }
                : roles.ToList();

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(order => order.UserId == user.Id)
                .Include(order => order.OrderDetails)
                    .ThenInclude(detail => detail.Product)
                .OrderByDescending(order => order.OrderDate)
                .ToListAsync();

            var deliveredOrders = orders.Count(IsDelivered);

            var model = new AdminUserDetailsViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnd = user.LockoutEnd,
                Roles = displayRoles,
                Orders = orders,
                TotalOrders = orders.Count,
                DeliveredOrders = deliveredOrders,
                CancelledOrders = orders.Count(order =>
                    string.Equals(
                        order.Status,
                        "Cancelled",
                        StringComparison.OrdinalIgnoreCase)),
                TotalSpent = orders
                    .Where(IsDelivered)
                    .Sum(order => order.TotalPrice)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest();
            }

            await EnsureDefaultRolesAsync();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new AdminEditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                Role = roles.Contains(nameof(Roles.Admin))
                    ? nameof(Roles.Admin)
                    : nameof(Roles.User)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminEditUserViewModel model)
        {
            await EnsureDefaultRolesAsync();

            var validRoles = new[]
            {
                nameof(Roles.User),
                nameof(Roles.Admin)
            };

            if (!validRoles.Contains(model.Role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Vai trò không hợp lệ.");
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            var currentAdminId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var currentRoles =
                await _userManager.GetRolesAsync(user);

            var isCurrentlyAdmin =
                currentRoles.Contains(nameof(Roles.Admin));

            var willRemainAdmin =
                model.Role == nameof(Roles.Admin);

            if (user.Id == currentAdminId && !willRemainAdmin)
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Bạn không thể tự gỡ quyền Admin của chính mình.");
            }

            if (isCurrentlyAdmin && !willRemainAdmin)
            {
                var admins = await _userManager
                    .GetUsersInRoleAsync(nameof(Roles.Admin));

                if (admins.Count <= 1)
                {
                    ModelState.AddModelError(
                        nameof(model.Role),
                        "Hệ thống phải còn ít nhất một tài khoản Admin.");
                }
            }

            var normalizedEmail = model.Email.Trim();
            var userWithSameEmail =
                await _userManager.FindByEmailAsync(normalizedEmail);

            if (userWithSameEmail != null &&
                userWithSameEmail.Id != user.Id)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email này đã được một tài khoản khác sử dụng.");
            }

            var wantsPasswordChange =
                !string.IsNullOrWhiteSpace(model.NewPassword) ||
                !string.IsNullOrWhiteSpace(model.ConfirmPassword);

            if (wantsPasswordChange)
            {
                if (string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    ModelState.AddModelError(
                        nameof(model.NewPassword),
                        "Vui lòng nhập mật khẩu mới.");
                }

                if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
                {
                    ModelState.AddModelError(
                        nameof(model.ConfirmPassword),
                        "Vui lòng xác nhận mật khẩu mới.");
                }

                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    foreach (var validator in _userManager.PasswordValidators)
                    {
                        var validationResult = await validator.ValidateAsync(
                            _userManager,
                            user,
                            model.NewPassword);

                        if (!validationResult.Succeeded)
                        {
                            foreach (var error in validationResult.Errors)
                            {
                                ModelState.AddModelError(
                                    nameof(model.NewPassword),
                                    error.Description);
                            }
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                user.Email = normalizedEmail;
                user.UserName = normalizedEmail;
                user.PhoneNumber =
                    string.IsNullOrWhiteSpace(model.PhoneNumber)
                        ? null
                        : model.PhoneNumber.Trim();
                user.EmailConfirmed = model.EmailConfirmed;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    AddIdentityErrors(updateResult);
                    await transaction.RollbackAsync();
                    return View(model);
                }

                var rolesToRemove = currentRoles
                    .Where(currentRole =>
                        !string.Equals(
                            currentRole,
                            model.Role,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (rolesToRemove.Count > 0)
                {
                    var removeResult = await _userManager
                        .RemoveFromRolesAsync(user, rolesToRemove);

                    if (!removeResult.Succeeded)
                    {
                        AddIdentityErrors(removeResult);
                        await transaction.RollbackAsync();
                        return View(model);
                    }
                }

                if (!await _userManager.IsInRoleAsync(user, model.Role))
                {
                    var addResult = await _userManager
                        .AddToRoleAsync(user, model.Role);

                    if (!addResult.Succeeded)
                    {
                        AddIdentityErrors(addResult);
                        await transaction.RollbackAsync();
                        return View(model);
                    }
                }

                if (wantsPasswordChange)
                {
                    var resetToken = await _userManager
                        .GeneratePasswordResetTokenAsync(user);

                    var resetResult = await _userManager.ResetPasswordAsync(
                        user,
                        resetToken,
                        model.NewPassword!);

                    if (!resetResult.Succeeded)
                    {
                        AddIdentityErrors(resetResult);
                        await transaction.RollbackAsync();
                        return View(model);
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Không thể cập nhật tài khoản. Vui lòng thử lại.");

                return View(model);
            }

            TempData["Success"] = wantsPasswordChange
                ? $"Đã cập nhật tài khoản và mật khẩu của {user.Email}."
                : $"Đã cập nhật tài khoản {user.Email}.";

            return RedirectToAction(
                nameof(Details),
                new { id = user.Id });
        }

        private async Task EnsureDefaultRolesAsync()
        {
            foreach (var roleName in new[]
                     {
                         nameof(Roles.User),
                         nameof(Roles.Admin)
                     })
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var result = await _roleManager.CreateAsync(
                        new IdentityRole(roleName));

                    if (!result.Succeeded)
                    {
                        var errors = string.Join(
                            "; ",
                            result.Errors.Select(error => error.Description));

                        throw new InvalidOperationException(
                            $"Không thể tạo role {roleName}: {errors}");
                    }
                }
            }
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }
        }

        private static bool IsDelivered(Order order)
        {
            return string.Equals(
                       order.Status,
                       "Delivered",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       order.ShippingStatus,
                       "delivered",
                       StringComparison.OrdinalIgnoreCase);
        }

        private sealed class UserOrderStats
        {
            public string UserId { get; set; } = string.Empty;
            public int OrderCount { get; set; }
            public int DeliveredOrderCount { get; set; }
            public decimal TotalSpent { get; set; }
        }
    }
}
