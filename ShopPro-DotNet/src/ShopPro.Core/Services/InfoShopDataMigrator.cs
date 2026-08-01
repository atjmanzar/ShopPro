using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace ShopPro.Core.Services
{
    public class InfoShopImportProductDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = "General";
        public string Brand { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public decimal TaxRate { get; set; } = 18.00m;
        public int StockQuantity { get; set; }
        public int ReorderLevel { get; set; } = 5;
    }

    public class MigrationResult
    {
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> ErrorLogs { get; set; } = new();
    }

    public class InfoShopDataMigrator
    {
        private readonly ShopDbContext _db;

        public InfoShopDataMigrator(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<MigrationResult> ImportProductsFromJsonAsync(string jsonContent)
        {
            var result = new MigrationResult();
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                result.ErrorLogs.Add("Import failed: Content is empty.");
                return result;
            }

            try
            {
                var dtos = JsonSerializer.Deserialize<List<InfoShopImportProductDto>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dtos == null || !dtos.Any())
                {
                    result.ErrorLogs.Add("Import failed: No valid product items found in JSON array.");
                    return result;
                }

                return await ProcessImportDtosAsync(dtos);
            }
            catch (Exception ex)
            {
                result.ErrorLogs.Add($"JSON Parsing Error: {ex.Message}");
                return result;
            }
        }

        public async Task<MigrationResult> ImportProductsFromCsvAsync(string csvContent)
        {
            var result = new MigrationResult();
            if (string.IsNullOrWhiteSpace(csvContent))
            {
                result.ErrorLogs.Add("Import failed: CSV Content is empty.");
                return result;
            }

            try
            {
                var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length <= 1)
                {
                    result.ErrorLogs.Add("Import failed: CSV has no data rows.");
                    return result;
                }

                var header = lines[0].Split(',').Select(h => h.Trim().ToLower()).ToList();
                int skuIdx = header.IndexOf("sku");
                int barcodeIdx = header.IndexOf("barcode");
                int nameIdx = header.IndexOf("name");
                int catIdx = header.IndexOf("categoryname");
                if (catIdx == -1) catIdx = header.IndexOf("category");
                int brandIdx = header.IndexOf("brand");
                int priceIdx = header.IndexOf("price");
                int costIdx = header.IndexOf("cost");
                int taxIdx = header.IndexOf("taxrate");
                int stockIdx = header.IndexOf("stockquantity");
                if (stockIdx == -1) stockIdx = header.IndexOf("stock");
                int reorderIdx = header.IndexOf("reorderlevel");

                var dtos = new List<InfoShopImportProductDto>();
                for (int i = 1; i < lines.Length; i++)
                {
                    var cols = lines[i].Split(',');
                    if (cols.Length < 3) continue;

                    var dto = new InfoShopImportProductDto
                    {
                        Sku = skuIdx >= 0 && skuIdx < cols.Length ? cols[skuIdx].Trim() : string.Empty,
                        Barcode = barcodeIdx >= 0 && barcodeIdx < cols.Length ? cols[barcodeIdx].Trim() : string.Empty,
                        Name = nameIdx >= 0 && nameIdx < cols.Length ? cols[nameIdx].Trim() : string.Empty,
                        CategoryName = catIdx >= 0 && catIdx < cols.Length ? cols[catIdx].Trim() : "General",
                        Brand = brandIdx >= 0 && brandIdx < cols.Length ? cols[brandIdx].Trim() : string.Empty,
                    };

                    if (priceIdx >= 0 && priceIdx < cols.Length && decimal.TryParse(cols[priceIdx].Trim(), out var p)) dto.Price = p;
                    if (costIdx >= 0 && costIdx < cols.Length && decimal.TryParse(cols[costIdx].Trim(), out var c)) dto.Cost = c;
                    if (taxIdx >= 0 && taxIdx < cols.Length && decimal.TryParse(cols[taxIdx].Trim(), out var t)) dto.TaxRate = t;
                    if (stockIdx >= 0 && stockIdx < cols.Length && int.TryParse(cols[stockIdx].Trim(), out var s)) dto.StockQuantity = s;
                    if (reorderIdx >= 0 && reorderIdx < cols.Length && int.TryParse(cols[reorderIdx].Trim(), out var r)) dto.ReorderLevel = r;

                    dtos.Add(dto);
                }

                return await ProcessImportDtosAsync(dtos);
            }
            catch (Exception ex)
            {
                result.ErrorLogs.Add($"CSV Parsing Error: {ex.Message}");
                return result;
            }
        }

        private async Task<MigrationResult> ProcessImportDtosAsync(List<InfoShopImportProductDto> dtos)
        {
            var result = new MigrationResult();
            var batchBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var batchSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dto in dtos)
            {
                // Validation 1: Required fields
                if (string.IsNullOrWhiteSpace(dto.Barcode) || string.IsNullOrWhiteSpace(dto.Sku) || string.IsNullOrWhiteSpace(dto.Name))
                {
                    result.SkippedCount++;
                    result.ErrorLogs.Add($"Skipped entry with missing required fields: Name='{dto.Name}', SKU='{dto.Sku}', Barcode='{dto.Barcode}'");
                    continue;
                }

                // Validation 2: Price must be strictly positive (> 0)
                if (dto.Price <= 0)
                {
                    result.SkippedCount++;
                    result.ErrorLogs.Add($"Skipped entry '{dto.Name}' (SKU: {dto.Sku}): Price must be greater than 0 (Invalid Price: {dto.Price}).");
                    continue;
                }

                // Validation 3: Cost cannot be negative
                if (dto.Cost < 0)
                {
                    result.SkippedCount++;
                    result.ErrorLogs.Add($"Skipped entry '{dto.Name}' (SKU: {dto.Sku}): Cost cannot be negative ({dto.Cost}).");
                    continue;
                }

                // Validation 4: Intra-batch deduplication
                if (batchBarcodes.Contains(dto.Barcode) || batchSkus.Contains(dto.Sku))
                {
                    result.SkippedCount++;
                    result.ErrorLogs.Add($"Skipped intra-batch duplicate: Barcode='{dto.Barcode}' / SKU='{dto.Sku}'");
                    continue;
                }

                // Validation 5: Database deduplication
                bool existsInDb = await _db.Products.AnyAsync(p => p.Barcode == dto.Barcode || p.Sku == dto.Sku);
                if (existsInDb)
                {
                    result.SkippedCount++;
                    result.ErrorLogs.Add($"Skipped database collision: Barcode='{dto.Barcode}' or SKU='{dto.Sku}' already exists in database.");
                    continue;
                }

                // Get or create Category
                var categoryName = string.IsNullOrWhiteSpace(dto.CategoryName) ? "General" : dto.CategoryName.Trim();
                var category = await _db.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == categoryName.ToLower());
                if (category == null)
                {
                    category = new Category { Name = categoryName, Description = "Imported from InfoShop" };
                    _db.Categories.Add(category);
                    await _db.SaveChangesAsync();
                }

                var product = new Product
                {
                    Sku = dto.Sku.Trim(),
                    Barcode = dto.Barcode.Trim(),
                    Name = dto.Name.Trim(),
                    Brand = dto.Brand.Trim(),
                    CategoryId = category.Id,
                    Price = dto.Price,
                    Cost = dto.Cost,
                    TaxRate = dto.TaxRate >= 0 ? dto.TaxRate : 18.00m,
                    StockQuantity = dto.StockQuantity >= 0 ? dto.StockQuantity : 0,
                    MinStockAlert = dto.ReorderLevel > 0 ? dto.ReorderLevel : 5,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Products.Add(product);
                batchBarcodes.Add(dto.Barcode);
                batchSkus.Add(dto.Sku);
                result.ImportedCount++;
            }

            await _db.SaveChangesAsync();
            return result;
        }
    }
}
