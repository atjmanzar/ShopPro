using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class CheckoutWorkflowTests
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
        public async Task HoldAndResumeCart_PreservesCartItemsAndTotals()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);
            var holdService = new HeldSaleService(db);

            await pos.AddProductByBarcodeAsync("8901234567890"); // Add 1 item
            decimal initialTotal = pos.GrandTotal;

            // Act: Hold Cart
            var heldSale = await holdService.HoldCartAsync(1, pos.Cart, "John Doe", pos.LineSubtotal, pos.TotalDiscount, pos.TotalTax, pos.GrandTotal);
            pos.ClearCart();
            Assert.Empty(pos.Cart);

            // Act: Resume Cart
            var resumedCart = await holdService.ResumeHeldSaleAsync(heldSale.Id);
            pos.Cart.AddRange(resumedCart!);

            // Assert
            Assert.Single(pos.Cart);
            Assert.Equal(initialTotal, pos.GrandTotal);
        }

        [Fact]
        public async Task VoidSale_RestocksInventoryAndLogsTransaction()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);
            await pos.AddProductByBarcodeAsync("8901234567890");
            var product = await db.Products.FirstAsync(p => p.Barcode == "8901234567890");
            int initialStock = product.StockQuantity;

            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 100.00m);
            Assert.Equal(initialStock - 1, (await db.Products.FindAsync(product.Id))!.StockQuantity);

            // Act: Void Completed Sale
            var success = await pos.VoidSaleAsync(sale!.Id, 1, "Wrong Item Scanned");

            // Assert
            Assert.True(success);
            var restockedProduct = await db.Products.FindAsync(product.Id);
            Assert.Equal(initialStock, restockedProduct!.StockQuantity); // Restocked

            var voidLog = await db.InventoryTransactions.FirstOrDefaultAsync(t => t.Type == TransactionType.ReturnRestock);
            Assert.NotNull(voidLog);
            Assert.Contains("Void Sale", voidLog.Reason);
        }
    }
}
