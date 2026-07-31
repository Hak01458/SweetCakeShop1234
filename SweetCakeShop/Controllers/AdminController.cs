using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Services;
using SweetCakeShop.Models.ViewModels   ;
namespace SweetCakeShop.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly GhnService _ghnService;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment env, GhnService ghnService)
        {
            _context = context;
            _env = env;
            _ghnService = ghnService;
        }

        // Revenue / Reports
        [HttpGet]
        public async Task<IActionResult> Revenue(DateTime? startDate, DateTime? endDate, string? status, string? payment)
        {
            // default range: last 30 days
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = (endDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var q = _context.Orders.AsNoTracking().Where(o => o.OrderDate >= start && o.OrderDate <= end);

            if (!string.IsNullOrWhiteSpace(status))
            {
                q = q.Where(o => o.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(payment))
            {
                if (payment == "COD")
                    q = q.Where(o => o.Status == "COD");
                else if (payment == "Online")
                    q = q.Where(o => o.Status == "Confirmed" || o.Status == "Delivered" || o.Status == "Baked");
            }

            var orders = await q.OrderByDescending(o => o.OrderDate).ToListAsync();

            // aggregates
            var totalRevenue = orders.Sum(o => o.TotalPrice);
            var totalOrders = orders.Count;

            // breakdown by status
            var byStatus = orders.GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count(), Revenue = g.Sum(x => x.TotalPrice) })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            ViewBag.StartDate = start.Date.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.Date.ToString("yyyy-MM-dd");
            ViewBag.FilterStatus = status ?? string.Empty;
            ViewBag.FilterPayment = payment ?? string.Empty;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.ByStatus = byStatus;

            return View(orders);
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TopSellingProducts()
        {
            var ranking = await
                (from od in _context.OrderDetails.AsNoTracking()
                 join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                 join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                 where o.Status == "Delivered" || o.Status == "delivered"
                 group new { od, o, p } by new { od.ProductId, p.ProductName } into g
                 orderby g.Sum(x => x.od.Quantity) descending, g.Key.ProductName
                 select new AdminTopSellingProductViewModel
                 {
                     ProductId = g.Key.ProductId,
                     ProductName = g.Key.ProductName,
                     SoldQuantity = g.Sum(x => x.od.Quantity),
                     TotalRevenue = g.Sum(x => x.od.Quantity * x.od.Price),
                     DeliveredOrderCount = g.Select(x => x.o.OrderId).Distinct().Count()
                 })
                .ToListAsync();

            for (var i = 0; i < ranking.Count; i++)
            {
                ranking[i].Rank = i + 1;
            }

            return View(ranking);
        }

        #region Order Management
        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction(nameof(Orders));
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var validStatuses = new[] { "COD", "Confirmed", "Baked", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(status))
            {
                TempData["Error"] = "Trạng thái không hợp lệ";
                return RedirectToAction(nameof(Orders));
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction(nameof(Orders));
            }
            // Đơn đã giao thì không được thay đổi nữa
            if (order.Status == "Delivered")
            {
                TempData["Error"] = "Đơn hàng đã được giao, không thể chuyển trạng thái.";
                return RedirectToAction(nameof(Orders));
            }

            if (order.Status == "Cancelled")
            {
                TempData["Error"] = "Đơn hàng đã bị hủy, không thể chuyển trạng thái.";
                return RedirectToAction(nameof(Orders));
            }

            order.Status = status;
            if (status == "Delivered")
            {
                order.ShippingStatus = "delivered";

                if (order.DeliveredDate == null)
                {
                    order.DeliveredDate = DateTime.Now;
                }
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật trạng thái đơn #{order.OrderId}";
            return RedirectToAction(nameof(Orders));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction(nameof(Orders));
            }

            if (order.OrderDetails.Any())
            {
                _context.OrderDetails.RemoveRange(order.OrderDetails);
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa đơn hàng ";
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeCake(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng";
                return RedirectToAction(nameof(Orders));
            }

            if (!order.OrderDetails.Any())
            {
                TempData["Warning"] = "Không đủ nguyên liệu";
                return RedirectToAction(nameof(OrderDetails), new { orderId });
            }

            // Không làm lại cho đơn đã xác nhận/giao/hủy
            if (order.Status == "Baked" || order.Status == "Delivered" || order.Status == "Cancelled")
            {
                TempData["Error"] = "Đơn hàng không ở trạng thái có thể làm bánh";
                return RedirectToAction(nameof(OrderDetails), new { orderId });
            }

            var productIds = order.OrderDetails
                .Select(od => od.ProductId)
                .Distinct()
                .ToList();

            var recipes = await _context.Recipes
                .Where(r => productIds.Contains(r.ProductID))
                .ToListAsync();

            var recipesByProduct = recipes
                .GroupBy(r => r.ProductID)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Tính tổng nguyên liệu cần theo từng ingredient
            var requiredByIngredient = new Dictionary<int, decimal>();

            foreach (var detail in order.OrderDetails)
            {
                if (!recipesByProduct.TryGetValue(detail.ProductId, out var productRecipe) || productRecipe.Count == 0)
                {
                    TempData["Warning"] = "Không đủ nguyên liệu";
                    return RedirectToAction(nameof(OrderDetails), new { orderId });
                }

                foreach (var recipe in productRecipe)
                {
                    var required = recipe.Quantity * detail.Quantity;

                    if (requiredByIngredient.ContainsKey(recipe.IngredientsID))
                        requiredByIngredient[recipe.IngredientsID] += required;
                    else
                        requiredByIngredient[recipe.IngredientsID] = required;
                }
            }

            var ingredientIds = requiredByIngredient.Keys.ToList();
            var ingredients = await _context.Ingredients
                .Where(i => ingredientIds.Contains(i.IngredientID))
                .ToListAsync();

            // Có công thức nhưng thiếu dòng nguyên liệu tương ứng
            if (ingredients.Count != ingredientIds.Count)
            {
                TempData["Warning"] = "Không đủ nguyên liệu";
                return RedirectToAction(nameof(OrderDetails), new { orderId });
            }

            // Kiểm tra tồn kho đủ hay không
            foreach (var ingredient in ingredients)
            {
                var required = requiredByIngredient[ingredient.IngredientID];
                if (ingredient.Quantity < required)
                {
                    TempData["Warning"] = "Không đủ nguyên liệu";
                    return RedirectToAction(nameof(OrderDetails), new { orderId });
                }
            }

            // Trừ kho + cập nhật trạng thái
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var ingredient in ingredients)
                {
                    var required = requiredByIngredient[ingredient.IngredientID];
                    ingredient.Quantity = Math.Round(ingredient.Quantity - required, 2, MidpointRounding.AwayFromZero);
                }

                order.Status = "Baked";
                await _context.SaveChangesAsync();
                await _ghnService.CreateShippingOrderAsync(order);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = $"Đã làm bánh cho đơn #{order.OrderId}";
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra khi làm bánh";
            }

            return RedirectToAction(nameof(OrderDetails), new { orderId });
        }
        #endregion

        #region Category Management
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.CategoryId)
                .ToListAsync();

            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Tên danh mục không được để trống";
                return RedirectToAction(nameof(Categories));
            }

            var exists = await _context.Categories
                .AnyAsync(c => c.CategoryName == categoryName.Trim());

            if (exists)
            {
                TempData["Error"] = "Danh mục đã tồn tại";
                return RedirectToAction(nameof(Categories));
            }

            _context.Categories.Add(new Category { CategoryName = categoryName.Trim() });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm danh mục thành công";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(int categoryId, string categoryName)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục";
                return RedirectToAction(nameof(Categories));
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Tên danh mục không được để trống";
                return RedirectToAction(nameof(Categories));
            }

            category.CategoryName = categoryName.Trim();
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật danh mục thành công";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục";
                return RedirectToAction(nameof(Categories));
            }

            if (category.Products.Any())
            {
                TempData["Error"] = "Không thể xóa danh mục đang có sản phẩm";
                return RedirectToAction(nameof(Categories));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa danh mục thành công";
            return RedirectToAction(nameof(Categories));
        }
        #endregion

        #region Ingredients Management
        [HttpGet]
        public async Task<IActionResult> Ingredients()
        {
            var ingredients = await _context.Ingredients
                .OrderBy(i => i.Name)
                .ToListAsync();

            return View(ingredients);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIngredient(
            string name, decimal quantity, string measurement,
            DateTime? importDate, DateTime? expiryDate)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Tên nguyên liệu không được để trống";
                return RedirectToAction(nameof(Ingredients));
            }

            if (quantity < 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn hoặc bằng 0";
                return RedirectToAction(nameof(Ingredients));
            }

            if (string.IsNullOrWhiteSpace(measurement))
            {
                TempData["Error"] = "Đơn vị đo không được để trống";
                return RedirectToAction(nameof(Ingredients));
            }

            if (importDate.HasValue && expiryDate.HasValue && expiryDate <= importDate)
            {
                TempData["Error"] = "Ngày hết hạn phải sau ngày nhập";
                return RedirectToAction(nameof(Ingredients));
            }

            var existing = await _context.Ingredients
                .FirstOrDefaultAsync(i => i.Name == name.Trim());

            if (existing != null)
            {
                // Nguyên liệu đã có sẵn => coi đây là một đợt nhập hàng mới, cộng dồn số lượng
                if (!string.Equals(existing.Measurement, measurement.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = $"Nguyên liệu \"{existing.Name}\" đã tồn tại với đơn vị đo {existing.Measurement}";
                    return RedirectToAction(nameof(Ingredients));
                }

                existing.Quantity = Math.Round(existing.Quantity + quantity, 2, MidpointRounding.AwayFromZero);
                existing.ImportDate = importDate ?? existing.ImportDate;
                existing.ExpiryDate = expiryDate ?? existing.ExpiryDate;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cộng dồn {quantity} {measurement} vào tồn kho \"{existing.Name}\"";
                return RedirectToAction(nameof(Ingredients));
            }

            _context.Ingredients.Add(new Ingredient
            {
                Name = name.Trim(),
                Quantity = quantity,
                Measurement = measurement.Trim(),
                ImportDate = importDate,
                ExpiryDate = expiryDate
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm nguyên liệu thành công";
            return RedirectToAction(nameof(Ingredients));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIngredient(
            int ingredientId,
            string name,
            string measurement,
            decimal addAmount = 0,
            decimal subtractAmount = 0,
            DateTime? importDate = null, 
            DateTime? expiryDate = null)
        {
            var ingredient = await _context.Ingredients.FindAsync(ingredientId);
            if (ingredient == null)
            {
                TempData["Error"] = "Không tìm thấy nguyên liệu";
                return RedirectToAction(nameof(Ingredients));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Tên nguyên liệu không được để trống";
                return RedirectToAction(nameof(Ingredients));
            }

            if (string.IsNullOrWhiteSpace(measurement))
            {
                TempData["Error"] = "Đơn vị đo không được để trống";
                return RedirectToAction(nameof(Ingredients));
            }

            if (addAmount < 0 || subtractAmount < 0)
            {
                TempData["Error"] = "Giá trị Thêm/Trừ phải >= 0";
                return RedirectToAction(nameof(Ingredients));
            }

            addAmount = Math.Round(addAmount, 2, MidpointRounding.AwayFromZero);
            subtractAmount = Math.Round(subtractAmount, 2, MidpointRounding.AwayFromZero);

            var newQuantity = ingredient.Quantity + addAmount - subtractAmount;
            if (newQuantity < 0)
            {
                TempData["Error"] = "Số lượng không thể nhỏ hơn 0";
                return RedirectToAction(nameof(Ingredients));
            }

            ingredient.Name = name.Trim();
            ingredient.Measurement = measurement.Trim();
            ingredient.Quantity = Math.Round(newQuantity, 2, MidpointRounding.AwayFromZero);
            ingredient.ImportDate = importDate;
            ingredient.ExpiryDate = expiryDate;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật nguyên liệu thành công";
            return RedirectToAction(nameof(Ingredients));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIngredient(int ingredientId)
        {
            var ingredient = await _context.Ingredients.FindAsync(ingredientId);
            if (ingredient == null)
            {
                TempData["Error"] = "Không tìm thấy nguyên liệu";
                return RedirectToAction(nameof(Ingredients));
            }

            var isUsedInRecipe = await _context.Recipes
                .AnyAsync(r => r.IngredientsID == ingredientId);

            if (isUsedInRecipe)
            {
                TempData["Error"] = "Không thể xóa nguyên liệu vì vẫn đang được dùng trong công thức bánh";
                return RedirectToAction(nameof(Ingredients));
            }

            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa nguyên liệu thành công";
            return RedirectToAction(nameof(Ingredients));
        }
        #endregion

        #region Product Management
        [HttpGet]
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Categories = categories;

            var recipes = await _context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredient)
                .ToListAsync();

            var recipeByProduct = recipes
                .GroupBy(r => r.ProductID)
                .ToDictionary(g => g.Key, g => g.ToList());

            var model = new List<AdminProductStockViewModel>();

            foreach (var product in products)
            {
                var row = new AdminProductStockViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName
                };

                if (recipeByProduct.TryGetValue(product.ProductId, out var productRecipes))
                {
                    row.Ingredients = productRecipes
                        .Where(r => r.Ingredient != null && r.Quantity > 0)
                        .Select(r => new AdminRecipeIngredientViewModel
                        {
                            IngredientId = r.IngredientsID,
                            IngredientName = r.Ingredient!.Name,
                            InStock = r.Ingredient!.Quantity,
                            RequiredPerCake = r.Quantity,
                            Measurement = r.Ingredient!.Measurement
                        })
                        .ToList();
                }

                if (row.Ingredients.Count == 0)
                {
                    row.HasEnoughIngredients = false;
                    row.CanMakeCount = 0;
                }
                else
                {
                    row.HasEnoughIngredients = row.Ingredients.All(i => i.InStock >= i.RequiredPerCake);
                    row.CanMakeCount = (int)Math.Floor(row.Ingredients.Min(i => i.InStock / i.RequiredPerCake));
                }

                model.Add(row);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditProductRecipe(int productId)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy bánh";
                return RedirectToAction(nameof(Products));
            }

            var recipeItems = await _context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredient)
                .Where(r => r.ProductID == productId)
                .OrderBy(r => r.Ingredient!.Name)
                .Select(r => new AdminProductRecipeItemViewModel
                {
                    RecipeId = r.RecipeID,
                    IngredientId = r.IngredientsID,
                    IngredientName = r.Ingredient!.Name,
                    Measurement = r.Ingredient!.Measurement,
                    Quantity = r.Quantity
                })
                .ToListAsync();

            var ingredientOptions = await _context.Ingredients
                .AsNoTracking()
                .OrderBy(i => i.Name)
                .Select(i => new IngredientOptionViewModel
                {
                    IngredientId = i.IngredientID,
                    Name = i.Name,
                    Measurement = i.Measurement
                })
                .ToListAsync();

            var model = new AdminEditProductRecipeViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                RecipeItems = recipeItems,
                IngredientOptions = ingredientOptions
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIngredientToProduct(int productId, int ingredientId, decimal quantity)
        {
            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0";
                return RedirectToAction(nameof(EditProductRecipe), new { productId });
            }

            quantity = Math.Round(quantity, 2, MidpointRounding.AwayFromZero);

            var productExists = await _context.Products.AnyAsync(p => p.ProductId == productId);
            var ingredientExists = await _context.Ingredients.AnyAsync(i => i.IngredientID == ingredientId);

            if (!productExists || !ingredientExists)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ";
                return RedirectToAction(nameof(EditProductRecipe), new { productId });
            }

            var existing = await _context.Recipes
                .FirstOrDefaultAsync(r => r.ProductID == productId && r.IngredientsID == ingredientId);

            if (existing != null)
            {
                existing.Quantity = quantity; // nếu đã có thì cập nhật luôn
            }
            else
            {
                _context.Recipes.Add(new Recipe
                {
                    ProductID = productId,
                    IngredientsID = ingredientId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm/cập nhật nguyên liệu cho bánh";
            return RedirectToAction(nameof(EditProductRecipe), new { productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRecipeQuantity(int recipeId, int productId, decimal quantity)
        {
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.RecipeID == recipeId);
            if (recipe == null)
            {
                TempData["Error"] = "Không tìm thấy công thức";
                return RedirectToAction(nameof(EditProductRecipe), new { productId });
            }

            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0";
                return RedirectToAction(nameof(EditProductRecipe), new { productId });
            }

            recipe.Quantity = Math.Round(quantity, 2, MidpointRounding.AwayFromZero);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật định lượng thành công";
            return RedirectToAction(nameof(EditProductRecipe), new { productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveIngredientFromProduct(int recipeId, int productId)
        {
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.RecipeID == recipeId);
            if (recipe == null)
            {
                TempData["Error"] = "Không tìm thấy công thức";
                return RedirectToAction(nameof(EditProductRecipe), new { productId });
            }

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa nguyên liệu khỏi công thức bánh";
            return RedirectToAction(nameof(EditProductRecipe), new { productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(
            string productName,
            decimal price,
            int categoryId,
            string? description,
            IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                TempData["Error"] = "Tên bánh không được để trống";
                return RedirectToAction(nameof(Products));
            }

            if (price < 0)
            {
                TempData["Error"] = "Giá phải >= 0";
                return RedirectToAction(nameof(Products));
            }

            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
            if (!categoryExists)
            {
                TempData["Error"] = "Danh mục không hợp lệ";
                return RedirectToAction(nameof(Products));
            }

            var imagePath = await SaveProductImageAsync(imageFile);

            _context.Products.Add(new Product
            {
                ProductName = productName.Trim(),
                Price = price,
                CategoryId = categoryId,
                Description = description?.Trim(),
                Image = imagePath
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm bánh thành công";
            return RedirectToAction(nameof(Products));
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy bánh";
                return RedirectToAction(nameof(Products));
            }

            ViewBag.Categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(
            int productId,
            string productName,
            decimal price,
            int categoryId,
            string? description,
            IFormFile? imageFile)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy bánh";
                return RedirectToAction(nameof(Products));
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                TempData["Error"] = "Tên bánh không được để trống";
                return RedirectToAction(nameof(EditProduct), new { productId });
            }

            if (price < 0)
            {
                TempData["Error"] = "Giá phải >= 0";
                return RedirectToAction(nameof(EditProduct), new { productId });
            }

            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
            if (!categoryExists)
            {
                TempData["Error"] = "Danh mục không hợp lệ";
                return RedirectToAction(nameof(EditProduct), new { productId });
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                product.Image = await SaveProductImageAsync(imageFile);
            }

            product.ProductName = productName.Trim();
            product.Price = price;
            product.CategoryId = categoryId;
            product.Description = description?.Trim();

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật bánh thành công";
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy bánh";
                return RedirectToAction(nameof(Products));
            }

            // chặn xóa nếu đã có trong đơn hàng
            var usedInOrders = await _context.OrderDetails.AnyAsync(od => od.ProductId == productId);
            if (usedInOrders)
            {
                TempData["Error"] = "Không thể xóa bánh vì đã tồn tại trong đơn hàng";
                return RedirectToAction(nameof(Products));
            }

            // xóa công thức trước
            var recipes = await _context.Recipes.Where(r => r.ProductID == productId).ToListAsync();
            if (recipes.Count > 0)
            {
                _context.Recipes.RemoveRange(recipes);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa bánh thành công";
            return RedirectToAction(nameof(Products));
        }

        private async Task<string?> SaveProductImageAsync(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return null;

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(imageFile.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            return $"/uploads/products/{fileName}";
        }

        #endregion
    }
}
