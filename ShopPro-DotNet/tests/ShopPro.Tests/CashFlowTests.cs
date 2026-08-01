using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class CashFlowTests
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
        public async Task GetCashBookSummary_CalculatesCashInflowsAndOutflows()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);
            var expenseService = new ExpenseService(db);
            var cashFlow = new CashFlowService(db);

            // Cash Sale ₹48
            await pos.AddProductByBarcodeAsync("8901234567890");
            await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 100.00m);

            // Expense ₹10
            await expenseService.AddExpenseAsync("Supplies", "Paper Rolls", 10.00m, "", 1);

            // Act
            var summary = await cashFlow.GetCashBookSummaryAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

            // Assert
            Assert.Equal(48.00m, summary.CashSalesInflow);
            Assert.Equal(10.00m, summary.ExpensesOutflow);
            Assert.Equal(38.00m, summary.NetCashBalance); // ₹48 - ₹10
        }
    }
}
