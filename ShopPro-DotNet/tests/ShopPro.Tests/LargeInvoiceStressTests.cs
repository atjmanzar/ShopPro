using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Xunit;

namespace ShopPro.Tests
{
    public class LargeInvoiceStressTests
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
        public async Task LargeInvoiceCheckout_500Items_ExecutesUnder200Ms()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var generator = new CatalogStressTestGenerator(db);
            var pos = new PosEngine(db);

            await generator.SeedSyntheticProductsAsync(50);
            var products = await db.Products.Take(50).ToListAsync();

            // Populate cart with 500 line items
            for (int i = 0; i < 500; i++)
            {
                var p = products[i % products.Count];
                pos.Cart.Add(new ShopPro.Core.Models.CartItem
                {
                    Product = p,
                    Quantity = 2,
                    UnitPrice = p.Price,
                    TaxRate = p.TaxRate
                });
            }

            pos.InvoiceDiscountPercentage = 10.0m;

            // Act: Process checkout on 500 items
            var sw = Stopwatch.StartNew();
            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 1000000.00m);
            sw.Stop();

            // Assert
            Assert.NotNull(sale);
            Assert.Equal(500, sale.Items.Count);
            Assert.True(sw.ElapsedMilliseconds < 200, $"Large invoice checkout took {sw.ElapsedMilliseconds}ms (Expected < 200ms)");
        }
    }
}
