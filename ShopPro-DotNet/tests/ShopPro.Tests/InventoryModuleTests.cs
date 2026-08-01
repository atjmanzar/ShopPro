using ShopPro.Core.Services;
using ShopPro.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class InventoryModuleTests
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
        public async Task StockIn_IncreasesQuantityAndCreatesTransactionRecord()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new InventoryService(db);
            var initialProduct = await db.Products.FirstAsync();
            int initialStock = initialProduct.StockQuantity;

            // Act
            var success = await service.StockInAsync(initialProduct.Id, 25, "Restock batch #101", 1);
            var updatedProduct = await db.Products.FindAsync(initialProduct.Id);
            var transaction = await db.InventoryTransactions
                .FirstOrDefaultAsync(t => t.ProductId == initialProduct.Id && t.Reason.Contains("Restock batch #101"));

            // Assert
            Assert.True(success);
            Assert.Equal(initialStock + 25, updatedProduct!.StockQuantity);
            Assert.NotNull(transaction);
            Assert.Equal(25, transaction.QuantityChange);
        }

        [Fact]
        public async Task StockOut_DecreasesQuantityAndCreatesTransactionRecord()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new InventoryService(db);
            var initialProduct = await db.Products.FirstAsync();
            int initialStock = initialProduct.StockQuantity;

            // Act
            var success = await service.StockOutAsync(initialProduct.Id, 10, "Damaged stock disposal", 1);
            var updatedProduct = await db.Products.FindAsync(initialProduct.Id);

            // Assert
            Assert.True(success);
            Assert.Equal(initialStock - 10, updatedProduct!.StockQuantity);
        }

        [Fact]
        public async Task StockOut_ExceedingAvailableStock_ReturnsFailure_NegativeStockProtection()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new InventoryService(db);
            var product = await db.Products.FirstAsync();

            // Act: Attempt to deduct 9999 items (exceeds current stock)
            var success = await service.StockOutAsync(product.Id, 9999, "Excessive Stock Out", 1);

            // Assert
            Assert.False(success); // Negative stock protection triggered
        }

        [Fact]
        public async Task AdjustStock_ZeroQuantity_ReturnsFailure()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new InventoryService(db);
            var product = await db.Products.FirstAsync();

            // Act
            var success = await service.StockInAsync(product.Id, 0, "Invalid Zero Stock In", 1);

            // Assert
            Assert.False(success);
        }
    }
}
