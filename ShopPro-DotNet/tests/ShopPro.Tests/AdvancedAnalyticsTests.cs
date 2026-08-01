using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class AdvancedAnalyticsTests
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
        public async Task GetProfitLossSummary_CalculatesGrossRevenueCogsAndNetProfit()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);
            var expenseService = new ExpenseService(db);
            var analytics = new AdvancedAnalyticsService(db);

            // Sale 1 product (Maggi: Price ₹48, Cost ₹38)
            await pos.AddProductByBarcodeAsync("8901234567890");
            await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 100.00m);

            // Record Store Expense ₹5.00
            await expenseService.AddExpenseAsync("Rent", "Shop Rent Share", 5.00m, "", 1);

            // Act
            var pl = await analytics.GetProfitLossSummaryAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

            // Assert
            Assert.Equal(48.00m, pl.GrossRevenue);
            Assert.Equal(38.00m, pl.TotalCogs);
            Assert.Equal(10.00m, pl.GrossProfit); // ₹48 - ₹38
            Assert.Equal(5.00m, pl.OperatingExpenses);
            Assert.Equal(5.00m, pl.NetProfit); // ₹10 Gross Profit - ₹5 Expense
        }
    }
}
