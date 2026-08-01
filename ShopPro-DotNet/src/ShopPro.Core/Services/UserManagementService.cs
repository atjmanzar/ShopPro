using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class UserManagementService
    {
        private readonly ShopDbContext _db;
        private static readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

        public UserManagementService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _db.Users.OrderBy(u => u.Username).ToListAsync();
        }

        public async Task<(bool Success, string Message)> CreateUserAsync(string username, string password, string fullName, UserRole role, bool canManageProducts, bool canGiveDiscount, bool canViewReports, bool canVoidSale)
        {
            if (string.IsNullOrWhiteSpace(username)) return (false, "Username cannot be empty.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 4) return (false, "Password must be at least 4 characters long.");

            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (existing != null) return (false, $"Username '{username}' is already taken.");

            var user = new User
            {
                Username = username.Trim(),
                FullName = fullName.Trim(),
                Role = role,
                CanManageProducts = canManageProducts,
                CanGiveDiscount = canGiveDiscount,
                CanViewReports = canViewReports,
                CanVoidSale = canVoidSale,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return (true, "User account created successfully.");
        }

        public async Task<(bool Success, string Message)> UpdateUserPermissionsAsync(int userId, UserRole role, bool canManageProducts, bool canGiveDiscount, bool canViewReports, bool canVoidSale, bool isActive)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.");

            user.Role = role;
            user.CanManageProducts = canManageProducts;
            user.CanGiveDiscount = canGiveDiscount;
            user.CanViewReports = canViewReports;
            user.CanVoidSale = canVoidSale;
            user.IsActive = isActive;

            await _db.SaveChangesAsync();
            return (true, "User settings and permissions updated.");
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string newPassword)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.");
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4) return (false, "New Password must be at least 4 characters long.");

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            await _db.SaveChangesAsync();
            return (true, $"Password for '{user.Username}' has been reset.");
        }
    }
}
