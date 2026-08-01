using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class EndToEndIntegrationTests
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
        public async Task CompleteRetailStoreLifecycle_ExecutesSuccessfully()
        {
            // Arrange Database
            using var db = CreateInMemoryDb();
            var auth = new AuthService(db);
            var pos = new PosEngine(db);
            var inventory = new InventoryService(db);
            var customerService = new CustomerLedgerService(db);
            var analytics = new AdvancedAnalyticsService(db);
            var licenseEngine = new LicenseValidationEngine(db);

            // 1. Authenticate Admin User
            var user = await auth.AuthenticateAsync("admin", "admin123");
            Assert.NotNull(user);

            // 2. Validate Commercial License / Trial
            var licenseState = await licenseEngine.ValidateCurrentLicenseAsync();
            Assert.True(licenseState.IsValid);

            // 3. Register Customer & Check Loyalty
            var customer = new Customer { Name = "E2E Test Customer", Tier = MembershipTier.Gold };
            await customerService.SaveCustomerAsync(customer);

            // 4. Perform POS Checkout (Product Scan + Cash Payment)
            await pos.AddProductByBarcodeAsync("8901234567890"); // Maggi 2-Minute Noodles
            var sale = await pos.ProcessCheckoutAsync(user.Id, PaymentMethod.Cash, 100.00m, customer.Id);
            Assert.NotNull(sale);

            // 5. Verify Stock Deduction in Inventory
            var product = await db.Products.FirstAsync(p => p.Barcode == "8901234567890");
            Assert.True(product.StockQuantity < 100);

            // 6. Verify Analytics & Profitability Report
            var pnl = await analytics.GetProfitAndLossSummaryAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));
            Assert.True(pnl.GrossRevenue > 0);
        }
    }
}
