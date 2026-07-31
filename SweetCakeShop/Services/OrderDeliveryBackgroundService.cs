using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;

namespace SweetCakeShop.Services
{
    public class OrderDeliveryBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public OrderDeliveryBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var now = DateTime.Now;

                var orders = await db.Orders
                    .Where(o =>
                        o.Status != "Delivered" &&
                        o.ShippingStartTime != null &&
                        o.DeliverySimulationTime != null)
                    .ToListAsync(stoppingToken);

                foreach (var order in orders)
                {
                    var totalTime = order.DeliverySimulationTime!.Value - order.ShippingStartTime!.Value;

                    var elapsedTime = now - order.ShippingStartTime.Value;

                    double progress = elapsedTime.TotalSeconds / totalTime.TotalSeconds;

                    // Giới hạn từ 0 -> 1
                    progress = Math.Max(0, Math.Min(progress, 1));

                    if (progress >= 1)
                    {
                        order.Status = "Delivered";
                        order.ShippingStatus = "delivered";
                        order.DeliveredDate = now;
                    }
                    else if (progress >= 0.75)
                    {
                        order.ShippingStatus = "delivering";
                    }
                    else if (progress >= 0.5)
                    {
                        order.ShippingStatus = "transporting";
                    }
                    else if (progress >= 0.25)
                    {
                        order.ShippingStatus = "picking";
                    }
                    else
                    {
                        order.ShippingStatus = "ready_to_pick";
                    }
                }

                await db.SaveChangesAsync(stoppingToken);

                // Kiểm tra mỗi 15 giây
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }
}