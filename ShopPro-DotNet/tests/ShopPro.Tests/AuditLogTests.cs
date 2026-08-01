using ShopPro.Core.Services;
using ShopPro.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class AuditLogTests
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
        public async Task LogActivity_RecordsSecurityEventWithTimestamp()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new AuditLogService(db);

            // Act
            await service.LogActivityAsync("UserLogin", "User", "User 'admin' logged into workstation.", 1);
            var logs = await service.GetAuditLogsAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

            // Assert
            Assert.NotEmpty(logs);
            var log = logs.First();
            Assert.Equal("UserLogin", log.Action);
            Assert.Contains("admin", log.Details);
        }
    }
}
