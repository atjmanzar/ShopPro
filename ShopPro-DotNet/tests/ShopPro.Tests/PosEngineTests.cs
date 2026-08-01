using ShopPro.Core.Models;
using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class PosEngineTests
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
        public void CartItem_CalculatesSubtotalAndTaxCorrectly()
        {
            // Arrange
            var item = new CartItem
            {
                Product = new Product { Name = "Test Item", Price = 100.00m, TaxRate = 18.00m },
                Quantity = 2,
                UnitPrice = 100.00m,
                DiscountPercentage = 10.00m, // 10% discount
                TaxRate = 18.00m // 18% GST
            };

            // Act & Assert
            Assert.Equal(200.00m, item.RawSubtotal);
            Assert.Equal(20.00m, item.DiscountAmount); // 10% of 200
            Assert.Equal(180.00m, item.NetSubtotal);   // 200 - 20
            Assert.Equal(32.40m, item.TaxAmount);       // 18% of 180
            Assert.Equal(212.40m, item.LineTotal);     // 180 + 32.40
        }

        [Fact]
        public async Task AddProductByBarcode_AddsProductToCart()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var engine = new PosEngine(db);

            // Act
            var success = await engine.AddProductByBarcodeAsync("8901234567890");

            // Assert
            Assert.True(success);
            Assert.Single(engine.Cart);
            Assert.Equal("Maggi 2-Minute Noodles 280g Pack", engine.Cart[0].Product.Name);
        }

        [Fact]
        public async Task ProcessCheckout_DeductsStockAndCreatesSaleRecord()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var engine = new PosEngine(db);
            await engine.AddProductByBarcodeAsync("8901234567890"); // Initial stock: 120

            // Act
            var sale = await engine.ProcessCheckoutAsync(1, PaymentMethod.Cash, 100.00m);

            // Assert
            Assert.NotNull(sale);
            Assert.Empty(engine.Cart); // Cart cleared after successful checkout

            var updatedProduct = await db.Products.FirstAsync(p => p.Barcode == "8901234567890");
            Assert.Equal(119, updatedProduct.StockQuantity); // Stock deducted by 1

            var transaction = await db.InventoryTransactions.FirstOrDefaultAsync(t => t.ProductId == updatedProduct.Id);
            Assert.NotNull(transaction);
            Assert.Equal(-1, transaction.QuantityChange);
            Assert.Equal(TransactionType.SaleDeduction, transaction.Type);
        }
    }
}
