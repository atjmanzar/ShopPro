using ShopPro.Core.Services;
using ShopPro.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class BackupRecoveryTests
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
        public async Task CreateBackup_GeneratesTimestampedBackupFile()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new BackupRecoveryService(db);

            // Act
            var backup = await service.CreateBackupAsync("test");
            var available = service.GetAvailableBackups();

            // Assert
            Assert.NotNull(backup);
            Assert.Contains("shoppro_test_backup", backup.FileName);
            Assert.NotEmpty(available);
        }
    }
}
