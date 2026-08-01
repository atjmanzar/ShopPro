using ShopPro.Core.Services;
using ShopPro.Core.Models;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class HeldSaleServiceTests
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
        public async Task HoldCart_DoesNotDeductStock_And_ResumeRestoresExactCartState()
        {
            // Hand Calculation:
            // Maggi Noodles initial stock = 120 units.
            // Step 1: Hold Cart with 5 Maggi Noodles.
            // Verifies: Stock in DB MUST remain 120 (NO stock deduction on Hold).
            // Step 2: Resume Held Cart.
            // Verifies: Returned CartItem list has 1 item, Quantity = 5, UnitPrice = 48.00.

            using var db = CreateInMemoryDb();
            var service = new HeldSaleService(db);

            var maggi = await db.Products.FirstAsync(p => p.Sku == "SKU-MAGGI-70G");
            int initialStock = maggi.StockQuantity; // 120

            var cart = new List<CartItem>
            {
                new CartItem
                {
                    Product = maggi,
                    Quantity = 5,
                    UnitPrice = maggi.Price,
                    TaxRate = maggi.TaxRate
                }
            };

            // Act 1: Hold Cart
            var heldSale = await service.HoldCartAsync(1, cart, "John Doe", 240.00m, 0m, 43.20m, 283.20m);

            // Assert 1: Stock NOT deducted on hold
            var dbProductOnHold = await db.Products.FindAsync(maggi.Id);
            Assert.Equal(initialStock, dbProductOnHold!.StockQuantity); // Still 120

            // Act 2: Resume Held Cart
            var resumedCart = await service.ResumeHeldSaleAsync(heldSale.Id);

            // Assert 2: Exact cart state restored
            Assert.NotNull(resumedCart);
            Assert.Single(resumedCart);
            Assert.Equal(5, resumedCart[0].Quantity);
            Assert.Equal(48.00m, resumedCart[0].UnitPrice);

            // Assert 3: HeldSale record removed from DB after resume
            var heldInDb = await db.HeldSales.FindAsync(heldSale.Id);
            Assert.Null(heldInDb);
        }
    }
}
