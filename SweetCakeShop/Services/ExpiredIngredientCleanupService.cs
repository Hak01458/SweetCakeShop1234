using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;

namespace SweetCakeShop.Services
{
    /// <summary>
    /// Chạy nền, định kỳ quét bảng Ingredients để xử lý các nguyên liệu đã hết hạn:
    /// - Nếu không còn được dùng trong công thức bánh nào -> xóa hẳn.
    /// - Nếu vẫn đang dùng trong công thức -> đưa số lượng về 0 (không xóa, tránh vỡ dữ liệu công thức).
    /// </summary>
    public class ExpiredIngredientCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpiredIngredientCleanupService> _logger;

        // Khoảng thời gian giữa 2 lần quét. Đổi lại tuỳ ý (ví dụ TimeSpan.FromHours(24) cho môi trường thật).
        private readonly TimeSpan _interval = TimeSpan.FromHours(24);

        public ExpiredIngredientCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<ExpiredIngredientCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredIngredientsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý nguyên liệu hết hạn");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task CleanupExpiredIngredientsAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.Today;

            var expiredIngredients = await context.Ingredients
                .Where(i => i.ExpiryDate.HasValue && i.ExpiryDate.Value.Date < today)
                .ToListAsync(stoppingToken);

            if (expiredIngredients.Count == 0)
                return;

            var usedIngredientIds = await context.Recipes
                .Select(r => r.IngredientsID)
                .Distinct()
                .ToListAsync(stoppingToken);

            int deletedCount = 0;
            int resetCount = 0;

            foreach (var ingredient in expiredIngredients)
            {
                if (usedIngredientIds.Contains(ingredient.IngredientID))
                {
                    if (ingredient.Quantity != 0)
                    {
                        ingredient.Quantity = 0;
                        resetCount++;
                    }
                }
                else
                {
                    context.Ingredients.Remove(ingredient);
                    deletedCount++;
                }
            }

            if (deletedCount > 0 || resetCount > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation(
                    "Đã xóa {DeletedCount} nguyên liệu hết hạn, reset về 0 cho {ResetCount} nguyên liệu hết hạn đang dùng trong công thức",
                    deletedCount, resetCount);
            }
        }
    }
}