using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class ExpenseServiceTests
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
        public async Task AddExpense_CalculatesDateRangeTotalsCorrectly()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new ExpenseService(db);

            // Act
            await service.AddExpenseAsync("Utilities", "Electricity Bill", 1200.00m, "Monthly power bill", 1);
            await service.AddExpenseAsync("Maintenance", "AC Repair", 800.00m, "", 1);

            var total = await service.GetTotalExpensesAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

            // Assert
            Assert.Equal(2000.00m, total);
        }
    }
}
