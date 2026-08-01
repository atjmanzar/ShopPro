using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class ProductModuleTests
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
        public async Task SaveProduct_ValidData_CreatesProductSuccessfully()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new ProductManagementService(db);

            var validProduct = new Product
            {
                Sku = "VALID-SKU-001",
                Barcode = "8901112223334",
                Name = "Valid Product Test",
                Price = 150.00m,
                Cost = 90.00m,
                TaxRate = 18.00m,
                StockQuantity = 20,
                CategoryId = 1
            };

            // Act
            var result = await service.SaveProductAsync(validProduct);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Product saved successfully.", result.Message);
            Assert.True(validProduct.Id > 0);
        }

        [Fact]
        public async Task SaveProduct_DuplicateBarcode_ReturnsFailure()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new ProductManagementService(db);

            var duplicateProduct = new Product
            {
                Sku = "NEW-SKU-999",
                Barcode = "8901234567890", // Duplicate barcode
                Name = "Duplicate Product Test",
                Price = 99.00m,
                Cost = 50.00m,
                CategoryId = 1
            };

            // Act
            var result = await service.SaveProductAsync(duplicateProduct);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Duplicate Barcode", result.Message);
        }

        [Fact]
        public async Task SaveProduct_DuplicateSku_ReturnsFailure()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new ProductManagementService(db);

            var duplicateSkuProduct = new Product
            {
                Sku = "SKU-MAGGI-70G", // Existing SKU
                Barcode = "999000111222",
                Name = "Duplicate SKU Product Test",
                Price = 45.00m,
                Cost = 25.00m,
                CategoryId = 1
            };

            // Act
            var result = await service.SaveProductAsync(duplicateSkuProduct);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Duplicate SKU", result.Message);
        }

        [Fact]
        public async Task SaveProduct_InvalidPriceZeroOrNegative_ReturnsFailure()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new ProductManagementService(db);

            var invalidPriceProduct = new Product
            {
                Sku = "FREE-001",
                Barcode = "999888777666",
                Name = "Invalid Zero Price Test",
                Price = 0.00m, // Invalid price <= 0
                Cost = 10.00m,
                CategoryId = 1
            };

            // Act
            var result = await service.SaveProductAsync(invalidPriceProduct);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid Price", result.Message);
        }

        [Fact]
        public async Task SaveProduct_NegativeCost_ReturnsFailure()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new ProductManagementService(db);

            var invalidCostProduct = new Product
            {
                Sku = "NEG-COST-001",
                Barcode = "999777666555",
                Name = "Negative Cost Test",
                Price = 100.00m,
                Cost = -10.00m, // Invalid negative cost
                CategoryId = 1
            };

            // Act
            var result = await service.SaveProductAsync(invalidCostProduct);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cost Price cannot be negative", result.Message);
        }

        [Fact]
        public async Task SaveProduct_NegativeStockQuantity_ReturnsFailure()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new ProductManagementService(db);

            var invalidStockProduct = new Product
            {
                Sku = "NEG-STOCK-001",
                Barcode = "999555444333",
                Name = "Negative Stock Test",
                Price = 100.00m,
                Cost = 50.00m,
                StockQuantity = -5, // Invalid negative stock
                CategoryId = 1
            };

            // Act
            var result = await service.SaveProductAsync(invalidStockProduct);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Stock Quantity cannot be negative", result.Message);
        }
    }
}
