using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class SupplierServiceTests
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
        public async Task SaveSupplier_CreatesVendorRecord()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new SupplierService(db);

            var supplier = new Supplier
            {
                CompanyName = "FMCG Distributors India Pvt Ltd",
                ContactPerson = "Rajesh Sharma",
                Phone = "9876543210",
                Gstin = "27ABCDE1234F1Z5"
            };

            // Act
            var result = await service.SaveSupplierAsync(supplier);
            var saved = await db.Suppliers.FirstOrDefaultAsync(s => s.CompanyName == "FMCG Distributors India Pvt Ltd");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(saved);
            Assert.Equal("9876543210", saved.Phone);
        }
    }
}
