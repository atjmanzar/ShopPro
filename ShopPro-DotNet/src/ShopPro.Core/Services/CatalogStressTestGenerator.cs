using ShopPro.Data;
using ShopPro.Data.Entities;

namespace ShopPro.Core.Services
{
    public class CatalogStressTestGenerator
    {
        private readonly ShopDbContext _db;

        public CatalogStressTestGenerator(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<int> SeedSyntheticProductsAsync(int count = 1000)
        {
            var category = new Category { Name = "Stress Test Category" };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            var products = new List<Product>();
            for (int i = 1; i <= count; i++)
            {
                products.Add(new Product
                {
                    Sku = $"STRESS-SKU-{i:D6}",
                    Barcode = $"890999{i:D7}",
                    Name = $"Synthetic Stress Product #{i}",
                    Brand = "Benchmark Brand",
                    CategoryId = category.Id,
                    Price = 100.00m + (i % 50),
                    Cost = 70.00m,
                    TaxRate = 18.00m,
                    StockQuantity = 500,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            _db.Products.AddRange(products);
            await _db.SaveChangesAsync();
            return count;
        }
    }
}
