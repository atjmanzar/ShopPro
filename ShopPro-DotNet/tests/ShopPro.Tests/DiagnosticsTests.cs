using ShopPro.Core.Services;
using ShopPro.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class DiagnosticsTests
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
        public async Task GetHealthReport_ReturnsValidRamAndDatabaseMetrics()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var monitor = new SystemHealthMonitor(db);

            // Act
            var report = await monitor.GetHealthReportAsync();

            // Assert
            Assert.NotNull(report);
            Assert.True(report.MemoryWorkingSetMb > 0);
            Assert.True(report.IsDatabaseConnected);
            Assert.True(report.DatabasePingLatencyMs >= 0);
        }

        [Fact]
        public void ExceptionLogger_LogsExceptionStackTraces()
        {
            // Arrange
            var testEx = new InvalidOperationException("Test exception for diagnostics suite");

            // Act
            ExceptionLogger.LogException(testEx, "Unit Testing Context");
            var logs = ExceptionLogger.GetRecentLogContent();

            // Assert
            Assert.Contains("Test exception for diagnostics suite", logs);
            Assert.Contains("InvalidOperationException", logs);
        }
    }
}
