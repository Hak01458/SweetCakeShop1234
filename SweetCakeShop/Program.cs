    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using SweetCakeShop.Data;
    using SweetCakeShop.Services;
    using Stripe;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using SweetCakeShop.Configurations;

namespace SweetCakeShop
    {
        public class Program
        {
            public static void Main(string[] args)
            {
            
                var builder = WebApplication.CreateBuilder(args);
                builder.Services.Configure<GhnSettings>(
        builder.Configuration.GetSection("GHN"));


                // Add services to the container.
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString));
                builder.Services.AddDatabaseDeveloperPageExceptionFilter();

                builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.Configure<EmailSettings>(
     builder.Configuration.GetSection("EmailSettings"));

            builder.Services.AddTransient<IEmailSender, GmailEmailSender>();
            // Configure Identity cookie so "Remember me" creates a persistent cookie
            builder.Services.ConfigureApplicationCookie(options =>
                {
                    // How long the persistent cookie (when RememberMe = true) will persist
                    // Set to a very long duration (100 years) to effectively remove a practical expiration.
                    // Note: keeping authentication cookies without reasonable expiry is a security and privacy risk.
                    options.ExpireTimeSpan = TimeSpan.FromDays(36500);
                    options.SlidingExpiration = true;

                    // Useful paths (adjust if your identity routes differ)
                    options.LoginPath = "/Identity/Account/Login";
                    options.LogoutPath = "/Identity/Account/Logout";
                    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

                    options.Cookie.HttpOnly = true;
                    // options.Cookie.IsEssential = true; // uncomment if you want cookie to be considered essential for GDPR scenarios
                });
                builder.Services.AddHttpClient<GhnService>();
                builder.Services.AddControllersWithViews();
            builder.Services.AddHostedService<OrderDeliveryBackgroundService>();
            builder.Services.AddRazorPages();

                // Session and cart registration
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddSession(options =>
                {
                    options.IdleTimeout = TimeSpan.FromHours(2);
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                });
                builder.Services.AddScoped<CartService>();
                builder.Services.AddScoped<OrderService>();
            builder.Services.AddScoped<OrderEmailService>();
            builder.Services.AddScoped<CustomerLoyaltyService>();
            // Session and cart registration
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            builder.Services.AddScoped<CartService>();
            builder.Services.AddHostedService<ExpiredIngredientCleanupService>();
            builder.Services.AddScoped<OrderService>();

                // Payment service registration (DI). Keep HttpClient for backward compatibility
                builder.Services.AddHttpClient<IPaymentService, PaymentService>();

                // Configure Stripe API key from configuration or environment
                // Put your secret key into environment variable or user secrets: "Stripe:SecretKey"
                var stripeSecret = builder.Configuration["Stripe:SecretKey"]
                                   ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
                if (!string.IsNullOrWhiteSpace(stripeSecret))
                {
                    StripeConfiguration.ApiKey = stripeSecret;
                }
            builder.Services.Configure<VnPaySettings>(
            builder.Configuration.GetSection("VnPay"));

            builder.Services.AddScoped<IVnPayService, VnPayService>();
            var app = builder.Build();

                // Seed database với dữ liệu mẫu
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        Console.WriteLine("Đang khởi tạo seed data...");
                        SeedData.Initialize(services);
                        IdentitySeed.SeedAdminAsync(services).GetAwaiter().GetResult();
                        Console.WriteLine("Seed data hoàn tất!");
                    }
                    catch (Exception ex)
                    {
                        var logger = services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "LỖI KHI SEED DỮ LIỆU: {Message}", ex.Message);
                        throw;
                    }

                }

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseMigrationsEndPoint();
                }
                else
                {
                    app.UseExceptionHandler("/Home/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseRouting();               

                app.UseSession(); // <- enable session

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapStaticAssets();
                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}")
                    .WithStaticAssets();
                app.MapRazorPages().WithStaticAssets();

                app.Run();
            }
        }
    }
