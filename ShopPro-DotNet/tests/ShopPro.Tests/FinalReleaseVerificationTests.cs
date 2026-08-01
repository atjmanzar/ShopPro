using ShopPro.Core.Services;
using ShopPro.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class FinalReleaseVerificationTests
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
        public async Task FinalRelease_ZeroCloudDependency_ExecutesCompletelyOffline()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var auth = new AuthService(db);
            var pos = new PosEngine(db);
            var backup = new BackupRecoveryService(db);
            var license = new LicenseValidationEngine(db);

            // 1. Verify Offline Database Initialization
            Assert.True(await db.Products.AnyAsync());

            // 2. Verify Offline Authentication
            var user = await auth.AuthenticateAsync("admin", "admin123");
            Assert.NotNull(user);

            // 3. Verify Offline Trial / License Validation
            var licenseState = await license.ValidateCurrentLicenseAsync();
            Assert.True(licenseState.IsValid);

            // 4. Verify Local Backup Engine
            var backupInfo = await backup.CreateBackupAsync("final_release");
            Assert.NotNull(backupInfo);
            Assert.True(File.Exists(backupInfo.FilePath));
        }
    }
}
