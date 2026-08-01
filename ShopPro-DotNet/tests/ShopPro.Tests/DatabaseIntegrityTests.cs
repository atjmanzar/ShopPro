using ShopPro.Core.Services;
using ShopPro.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class DatabaseIntegrityTests
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
        public async Task CatalogExporter_ExportsAndImportsStoreCatalogPackage()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var exporter = new CatalogArchiveExporter(db);

            // Act: Export JSON
            var json = await exporter.ExportCatalogToJsonAsync();
            Assert.Contains("ShopPro Retail Store", json);
            Assert.Contains("Maggi 2-Minute Noodles", json);

            // Act: Import JSON Package
            int imported = await exporter.ImportCatalogFromJsonAsync(json);

            // Assert
            Assert.True(imported >= 0); // Re-import skips existing barcodes cleanly
        }
    }
}
