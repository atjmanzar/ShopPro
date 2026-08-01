using ShopPro.Core.Services;
using ShopPro.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class LicenseValidationTests
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
        public async Task ValidateCurrentLicense_NewInstallation_ReturnsActive14DayTrial()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var engine = new LicenseValidationEngine(db);

            // Act
            var state = await engine.ValidateCurrentLicenseAsync();

            // Assert
            Assert.True(state.IsValid);
            Assert.True(state.IsTrial);
            Assert.Equal(14, state.RemainingTrialDays);
        }

        [Fact]
        public async Task ActivateLicenseKey_ValidKey_ActivatesCommercialLicense()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var engine = new LicenseValidationEngine(db);

            // Act
            var result = await engine.ActivateLicenseKeyAsync("PRO-COMMERCIAL-999", "Acme Retailers");
            var state = await engine.ValidateCurrentLicenseAsync();

            // Assert
            Assert.True(result.Success);
            Assert.True(state.IsValid);
            Assert.False(state.IsTrial);
            Assert.Equal("Acme Retailers", state.CustomerName);
        }
    }
}
