using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ShopPro.Core.Services
{
    public class StoreCatalogExportPackage
    {
        public string StoreName { get; set; } = "ShopPro Retail Store";
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public List<Category> Categories { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
    }

    public class CatalogArchiveExporter
    {
        private readonly ShopDbContext _db;

        public CatalogArchiveExporter(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<string> ExportCatalogToJsonAsync()
        {
            var package = new StoreCatalogExportPackage
            {
                Categories = await _db.Categories.ToListAsync(),
                Products = await _db.Products.Include(p => p.Category).ToListAsync(),
                Customers = await _db.Customers.ToListAsync()
            };

            return JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task<int> ImportCatalogFromJsonAsync(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent)) return 0;

            var package = JsonSerializer.Deserialize<StoreCatalogExportPackage>(jsonContent);
            if (package == null) return 0;

            int imported = 0;
            foreach (var p in package.Products)
            {
                bool exists = await _db.Products.AnyAsync(existing => existing.Barcode == p.Barcode || existing.Sku == p.Sku);
                if (!exists)
                {
                    _db.Products.Add(new Product
                    {
                        Sku = p.Sku,
                        Barcode = p.Barcode,
                        Name = p.Name,
                        Brand = p.Brand,
                        CategoryId = p.CategoryId > 0 ? p.CategoryId : 1,
                        Price = p.Price,
                        Cost = p.Cost,
                        TaxRate = p.TaxRate,
                        StockQuantity = p.StockQuantity,
                        MinStockAlert = p.MinStockAlert,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    imported++;
                }
            }

            await _db.SaveChangesAsync();
            return imported;
        }
    }
}
