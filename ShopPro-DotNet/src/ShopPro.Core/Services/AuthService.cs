using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class AuthService
    {
        private readonly ShopDbContext _db;

        public User? CurrentUser { get; private set; }

        public AuthService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (user == null) return null;

            // Secure PBKDF2 Password Verification
            var isValid = DbInitializer.VerifyPassword(user, user.PasswordHash, password);
            if (!isValid) return null;

            CurrentUser = user;
            return user;
        }

        public void Logout()
        {
            CurrentUser = null;
        }

        public bool IsAdmin => CurrentUser?.Role == UserRole.Admin;
        public bool IsCashier => CurrentUser?.Role == UserRole.Cashier;
    }
}
