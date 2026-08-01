using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class AuthServiceTests
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
        public async Task Login_ValidAdminCredentials_ReturnsAdminUser()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var auth = new AuthService(db);

            // Act
            var user = await auth.LoginAsync("admin", "admin123");

            // Assert
            Assert.NotNull(user);
            Assert.Equal("admin", user.Username);
            Assert.Equal(UserRole.Admin, user.Role);
            Assert.True(auth.IsAdmin);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsNull()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var auth = new AuthService(db);

            // Act
            var user = await auth.LoginAsync("admin", "wrongpass");

            // Assert
            Assert.Null(user);
            Assert.Null(auth.CurrentUser);
        }
    }
}
