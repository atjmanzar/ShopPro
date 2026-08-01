using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class InventoryServiceTests
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
        public async Task AdjustStock_UpdatesQuantityAndLogsTransactionReason()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new InventoryService(db);
            var product = await db.Products.FirstAsync();
            int initialStock = product.StockQuantity;

            // Act
            var success = await service.AdjustStockAsync(product.Id, 25, "Supplier Restock Audit", 1);

            // Assert
            Assert.True(success);
            var updatedProduct = await db.Products.FindAsync(product.Id);
            Assert.Equal(initialStock + 25, updatedProduct?.StockQuantity);

            var auditLog = await db.InventoryTransactions.FirstOrDefaultAsync(t => t.ProductId == product.Id && t.Reason == "Supplier Restock Audit");
            Assert.NotNull(auditLog);
            Assert.Equal(25, auditLog.QuantityChange);
        }
    }
}
