using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class CustomerLedgerService
    {
        private readonly ShopDbContext _db;

        public CustomerLedgerService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _db.Customers.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<(bool Success, string Message)> SaveCustomerAsync(Customer customer)
        {
            if (string.IsNullOrWhiteSpace(customer.Name))
                return (false, "Customer Name cannot be empty.");

            if (customer.Id == 0)
            {
                customer.CreatedAt = DateTime.UtcNow;
                _db.Customers.Add(customer);
            }
            else
            {
                _db.Customers.Update(customer);
            }

            await _db.SaveChangesAsync();
            return (true, "Customer details saved.");
        }

        public async Task<bool> RecordCreditPaymentAsync(int customerId, decimal paymentAmount, string paymentMode, int userId)
        {
            var customer = await _db.Customers.FindAsync(customerId);
            if (customer == null || paymentAmount <= 0) return false;

            customer.CreditBalance = Math.Max(0m, customer.CreditBalance - paymentAmount);

            _db.AuditLogs.Add(new AuditLog
            {
                Action = "CustomerCreditPayment",
                TargetEntity = "Customer",
                Details = $"Received ₹{paymentAmount:N2} credit payment from '{customer.Name}' via {paymentMode}",
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<Sale>> GetCustomerPurchaseHistoryAsync(int customerId)
        {
            return await _db.Sales
                .Include(s => s.Items)
                .ThenInclude(i => i.Product)
                .Where(s => s.CustomerId == customerId)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }
    }
}
