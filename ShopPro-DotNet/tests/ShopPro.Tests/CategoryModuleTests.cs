using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class CategoryModuleTests
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
        public async Task SaveCategory_DuplicateNameCaseInsensitive_ReturnsFailure()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new ProductManagementService(db);

            var duplicateCat = new Category { Name = "GROCERY" }; // "Grocery" already exists in DbInitializer

            // Act
            var result = await service.SaveCategoryAsync(duplicateCat);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("already exists", result.Message);
        }

        [Fact]
        public async Task DeleteCategory_CategoryHasActiveProducts_BlocksDeletion()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new ProductManagementService(db);

            // Grocery category (Id 1) has active products assigned in DbInitializer

            // Act
            var result = await service.DeleteCategoryAsync(1);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot delete category", result.Message);
            Assert.Contains("active product(s) assigned", result.Message);
        }

        [Fact]
        public async Task DeleteCategory_CategoryHasNoProducts_DeletesSuccessfully()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new ProductManagementService(db);

            var emptyCat = new Category { Name = "Empty Unused Category" };
            await service.SaveCategoryAsync(emptyCat);

            // Act
            var result = await service.DeleteCategoryAsync(emptyCat.Id);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("deleted successfully", result.Message);
        }
    }
}
