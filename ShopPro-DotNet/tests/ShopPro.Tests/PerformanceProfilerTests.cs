using ShopPro.Core.Services;
using ShopPro.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class PerformanceProfilerTests
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
        public async Task DiagnosticRepairTool_ExecutesVacuumAndReindexSuccessfully()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var repairTool = new DiagnosticRepairTool(db);

            // Act
            var result = await repairTool.VacuumDatabaseAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("completed successfully", result.Details);
        }
    }
}
