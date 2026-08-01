using ShopPro.Core.Services;
using ShopPro.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Xunit;

namespace ShopPro.Tests
{
    public class StressTests100kCatalog
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
        public async Task BarcodeSearch_LargeCatalog_ExecutesUnder50Ms()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var generator = new CatalogStressTestGenerator(db);
            var pos = new PosEngine(db);

            await generator.SeedSyntheticProductsAsync(500); // Seed synthetic catalog

            // Act: Benchmark barcode search
            var sw = Stopwatch.StartNew();
            var success = await pos.AddProductByBarcodeAsync("8909990000250");
            sw.Stop();

            // Assert
            Assert.True(success);
            Assert.True(sw.ElapsedMilliseconds < 50, $"Query latency was {sw.ElapsedMilliseconds}ms (Expected < 50ms)");
        }
    }
}
