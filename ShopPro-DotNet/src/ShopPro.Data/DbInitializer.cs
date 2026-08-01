using ShopPro.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace ShopPro.Data
{
    public static class DbInitializer
    {
        private static readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

        public static void Initialize(ShopDbContext context)
        {
            context.Database.EnsureCreated();

            if (!context.Users.Any())
            {
                var admin = new User
                {
                    Username = "admin",
                    FullName = "System Administrator",
                    Role = UserRole.Admin
                };
                admin.PasswordHash = _passwordHasher.HashPassword(admin, "admin123");

                var cashier = new User
                {
                    Username = "cashier",
                    FullName = "Lead Cashier",
                    Role = UserRole.Cashier
                };
                cashier.PasswordHash = _passwordHasher.HashPassword(cashier, "cashier123");

                context.Users.AddRange(admin, cashier);
            }

            if (!context.Categories.Any())
            {
                var catGroceries = new Category { Name = "Grocery", Description = "Daily essential items" };
                var catBeverages = new Category { Name = "Beverages", Description = "Cold drinks, juices & water" };
                var catElectronics = new Category { Name = "Electronics", Description = "Accessories and gadgets" };

                context.Categories.AddRange(catGroceries, catBeverages, catElectronics);
                context.SaveChanges();

                context.Products.AddRange(
                    new Product
                    {
                        Sku = "SKU-MAGGI-70G",
                        Barcode = "8901234567890",
                        Name = "Maggi 2-Minute Noodles 280g Pack",
                        Category = catGroceries,
                        Price = 48.00m,
                        Cost = 38.00m,
                        TaxRate = 18.00m,
                        StockQuantity = 120,
                        IsActive = true
                    },
                    new Product
                    {
                        Sku = "BEV-001",
                        Barcode = "8901234567891",
                        Name = "Coca-Cola 750ml PET Bottle",
                        Category = catBeverages,
                        Price = 40.00m,
                        Cost = 30.00m,
                        TaxRate = 18.00m,
                        StockQuantity = 85,
                        IsActive = true
                    },
                    new Product
                    {
                        Sku = "ELEC-001",
                        Barcode = "8901234567892",
                        Name = "Type-C Fast Charging Cable 1.2m",
                        Category = catElectronics,
                        Price = 299.00m,
                        Cost = 150.00m,
                        TaxRate = 18.00m,
                        StockQuantity = 30,
                        IsActive = true
                    }
                );
            }

            context.SaveChanges();
        }

        public static string HashPassword(User user, string password)
        {
            return _passwordHasher.HashPassword(user, password);
        }

        public static bool VerifyPassword(User user, string hashedPassword, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
            return result != PasswordVerificationResult.Failed;
        }
    }
}
