using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class CustomerLedgerTests
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
        public async Task RecordCreditPayment_DeductsCustomerCreditBalance()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new CustomerLedgerService(db);

            var customer = new Customer { Name = "Credit Customer Test", CreditBalance = 500.00m };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            // Act
            var success = await service.RecordCreditPaymentAsync(customer.Id, 200.00m, "Cash", 1);
            var updated = await db.Customers.FindAsync(customer.Id);

            // Assert
            Assert.True(success);
            Assert.Equal(300.00m, updated!.CreditBalance); // ₹500 - ₹200
        }
    }
}
