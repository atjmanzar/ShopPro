using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class PurchasingEngineTests
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
        public async Task ProcessGrnReceipt_IncrementsProductStockAutomatically()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var supplierService = new SupplierService(db);
            var purchasing = new PurchasingEngine(db);

            var supplier = new Supplier { CompanyName = "Test Vendor" };
            await supplierService.SaveSupplierAsync(supplier);

            var product = await db.Products.FirstAsync();
            int initialStock = product.StockQuantity;

            // Act: Create Purchase Order (100 units @ ₹35 cost)
            var po = await purchasing.CreatePurchaseOrderAsync(supplier.Id, new List<(int, int, decimal)> { (product.Id, 100, 35.00m) }, "Restock PO");

            // Act: Process GRN Receipt
            var grnSuccess = await purchasing.ProcessGrnReceiptAsync(po.Id, 1);
            var updatedProduct = await db.Products.FindAsync(product.Id);

            // Assert
            Assert.True(grnSuccess);
            Assert.Equal(initialStock + 100, updatedProduct!.StockQuantity); // Stock Incremented automatically
            Assert.Equal(35.00m, updatedProduct.Cost); // Cost updated
        }
    }
}
