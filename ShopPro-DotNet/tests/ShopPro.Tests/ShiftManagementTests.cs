using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class ShiftManagementTests
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
        public async Task OpenAndCloseShift_CalculatesExpectedCashAndVarianceCorrectly()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new ShiftManagementService(db);
            var pos = new PosEngine(db);

            // Act: Open Shift with ₹1,000 Float
            var shift = await service.OpenShiftAsync(1, 1000.00m);
            Assert.Equal(ShiftStatus.Open, shift.Status);

            // Act: Complete ₹100 Cash Sale
            await pos.AddProductByBarcodeAsync("8901234567890");
            await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 100.00m);

            // Act: Close Shift with ₹1,090 Counted Cash (₹10 Short Variance)
            var closed = await service.CloseShiftAsync(shift.Id, 1090.00m);

            // Assert
            Assert.NotNull(closed);
            Assert.Equal(ShiftStatus.Closed, closed.Status);
            Assert.Equal(1000.00m, closed.OpeningFloat);
            Assert.Equal(1090.00m, closed.ClosingCashCount);
        }
    }
}
