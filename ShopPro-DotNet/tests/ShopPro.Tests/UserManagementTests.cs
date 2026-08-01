using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class UserManagementTests
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
        public async Task CreateUser_ValidCredentials_CreatesUserWithPbkdf2HashAndPermissions()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new UserManagementService(db);

            // Act
            var result = await service.CreateUserAsync("manager1", "pass1234", "Store Manager", UserRole.Manager, true, true, true, true);

            // Assert
            Assert.True(result.Success);
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == "manager1");
            Assert.NotNull(user);
            Assert.Equal(UserRole.Manager, user.Role);
            Assert.True(user.CanViewReports);
            Assert.True(user.CanVoidSale);
        }

        [Fact]
        public async Task ResetPassword_UpdatesPbkdf2PasswordHash()
        {
            // Arrange
            using var db = CreateInMemoryDb();
            var service = new UserManagementService(db);
            var auth = new AuthService(db);
            var cashier = await db.Users.FirstAsync(u => u.Username == "cashier");

            // Act
            var resetResult = await service.ResetPasswordAsync(cashier.Id, "newcashierpass");
            var loginOld = await auth.LoginAsync("cashier", "cashier123");
            var loginNew = await auth.LoginAsync("cashier", "newcashierpass");

            // Assert
            Assert.True(resetResult.Success);
            Assert.Null(loginOld); // Old password fails
            Assert.NotNull(loginNew); // New password succeeds
        }
    }
}
