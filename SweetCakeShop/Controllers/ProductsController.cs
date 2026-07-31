using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;
using X.PagedList;
using X.PagedList.Extensions;

namespace SweetCakeShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public IActionResult Index(string? sortOrder, string? searchTerm, int? page, int? categoryId, string? categoryName)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentSearch"] = searchTerm;
            ViewData["CurrentCategoryId"] = categoryId;
            ViewData["CurrentCategoryName"] = categoryName;

            var products = from p in _context.Products.Include(p => p.Category)
                           select p;
            //Hiện danh mục theo từng sản phẩm
            

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var categoryKeyword = categoryName.Trim();
                products = products.Where(p => p.Category != null && p.Category.CategoryName == categoryKeyword);
            }
            else if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }


            //tìm kiếm theo tên sản phẩm
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim();
                products = products.Where(p => p.ProductName.Contains(keyword));
            }

            // Sắp xếp theo giá
            products = sortOrder switch
            {
                "price_asc" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                _ => products.OrderBy(p => p.ProductId) // Mặc định
            };

            int pageSize = 12;
            int pageNumber = page ?? 1;
            var pagedProducts = products.ToPagedList(pageNumber, pageSize);
            
            return View("IndexPro", pagedProducts);
        }

        [HttpGet]
        public IActionResult IndexPro(string? sortOrder, string? searchTerm, int? page, int? categoryId, string? categoryName)
        {
            return Index(sortOrder, searchTerm, page, categoryId, categoryName);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            var similarProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                .OrderBy(p => p.ProductId)
                .Take(6)
                .ToListAsync();
            var reviews = await _context.ProductReviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var model = new ProductDetailsViewModel
            {
                Product = product,
                SimilarProducts = similarProducts,

                Reviews = reviews,

                TotalReviews = reviews.Count,

                AverageRating = reviews.Any()
        ? Math.Round(reviews.Average(r => r.Rating), 1)
        : 0
            };

            return View(model);
        }

    }
}
