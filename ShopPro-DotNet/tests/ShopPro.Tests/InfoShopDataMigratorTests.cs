using ShopPro.Core.Services;
using ShopPro.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class InfoShopDataMigratorTests
    {
        private ShopDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ShopDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            var db = new ShopDbContext(options);
            db.Database.OpenConnection();
            db.Database.EnsureCreated();

            DbInitializer.Initialize(db);
            return db;
        }

        [Fact]
        public async Task ImportProductsFromJson_ValidPayload_ImportsProductsAndCategories()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var migrator = new InfoShopDataMigrator(db);

            var json = @"[
                {
                    ""sku"": ""MIG-JSON-001"",
                    ""barcode"": ""8909876543210"",
                    ""name"": ""Migrated Soap 100g"",
                    ""categoryName"": ""Personal Care"",
                    ""brand"": ""CleanBrand"",
                    ""price"": 45.00,
                    ""cost"": 25.00,
                    ""taxRate"": 18.00,
                    ""stockQuantity"": 50
                }
            ]";

            // Act
            var result = await migrator.ImportProductsFromJsonAsync(json);

            // Assert
            Assert.Equal(1, result.ImportedCount);
            Assert.Equal(0, result.SkippedCount);

            var importedProduct = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Sku == "MIG-JSON-001");
            Assert.NotNull(importedProduct);
            Assert.Equal("Personal Care", importedProduct.Category?.Name);
        }

        [Fact]
        public async Task ImportProductsFromJson_IntraBatchDuplicates_SkipsDuplicates()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var migrator = new InfoShopDataMigrator(db);

            var json = @"[
                { ""sku"": ""BATCH-001"", ""barcode"": ""8901111111111"", ""name"": ""Batch Item 1"", ""price"": 50.00 },
                { ""sku"": ""BATCH-001"", ""barcode"": ""8901111111111"", ""name"": ""Batch Item Duplicate"", ""price"": 50.00 }
            ]";

            // Act
            var result = await migrator.ImportProductsFromJsonAsync(json);

            // Assert
            Assert.Equal(1, result.ImportedCount);
            Assert.Equal(1, result.SkippedCount); // 1 skipped due to intra-batch duplicate
        }

        [Fact]
        public async Task ImportProductsFromJson_InvalidPrice_SkipsInvalidEntryAndLogsError()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var migrator = new InfoShopDataMigrator(db);

            var json = @"[
                { ""sku"": ""FREE-ITEM"", ""barcode"": ""8902222222222"", ""name"": ""Free Invalid Item"", ""price"": 0.00 }
            ]";

            // Act
            var result = await migrator.ImportProductsFromJsonAsync(json);

            // Assert
            Assert.Equal(0, result.ImportedCount);
            Assert.Equal(1, result.SkippedCount);
            Assert.Contains(result.ErrorLogs, log => log.Contains("Price must be greater than 0"));
        }

        [Fact]
        public async Task ImportProductsFromCsv_ValidCsvContent_ImportsProductsSuccessfully()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var migrator = new InfoShopDataMigrator(db);

            var csv = "sku,barcode,name,categoryname,brand,price,cost,taxrate,stockquantity\n" +
                      "CSV-001,8903333333333,CSV Imported Biscuit,Snacks,CrispCo,30.00,18.00,18.00,100";

            // Act
            var result = await migrator.ImportProductsFromCsvAsync(csv);

            // Assert
            Assert.Equal(1, result.ImportedCount);
            Assert.Equal(0, result.SkippedCount);

            var imported = await db.Products.FirstOrDefaultAsync(p => p.Sku == "CSV-001");
            Assert.NotNull(imported);
            Assert.Equal(30.00m, imported.Price);
        }
    }
}
