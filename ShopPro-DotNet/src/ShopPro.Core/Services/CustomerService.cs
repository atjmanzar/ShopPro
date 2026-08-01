using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class CustomerService
    {
        private readonly ShopDbContext _db;

        public CustomerService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _db.Customers.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<Customer> AddCustomerAsync(string name, string phone, string email)
        {
            var customer = new Customer
            {
                Name = name,
                Phone = phone,
                Email = email,
                LoyaltyPoints = 0,
                CreatedAt = DateTime.UtcNow
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
            return customer;
        }

        public async Task AddLoyaltyPointsAsync(int customerId, decimal saleTotal)
        {
            var customer = await _db.Customers.FindAsync(customerId);
            if (customer != null)
            {
                // Earn 1 loyalty point for every ₹100 spent
                int pointsEarned = (int)(saleTotal / 100m);
                customer.LoyaltyPoints += pointsEarned;
                await _db.SaveChangesAsync();
            }
        }
    }
}
