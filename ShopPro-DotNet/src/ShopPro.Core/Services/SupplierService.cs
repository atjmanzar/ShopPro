using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class SupplierService
    {
        private readonly ShopDbContext _db;

        public SupplierService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<List<Supplier>> GetAllSuppliersAsync()
        {
            return await _db.Suppliers.OrderBy(s => s.CompanyName).ToListAsync();
        }

        public async Task<(bool Success, string Message)> SaveSupplierAsync(Supplier supplier)
        {
            if (string.IsNullOrWhiteSpace(supplier.CompanyName))
                return (false, "Company Name cannot be empty.");

            if (supplier.Id == 0)
            {
                supplier.CreatedAt = DateTime.UtcNow;
                _db.Suppliers.Add(supplier);
            }
            else
            {
                _db.Suppliers.Update(supplier);
            }

            await _db.SaveChangesAsync();
            return (true, "Supplier details saved.");
        }

        public async Task<bool> RecordSupplierPaymentAsync(int supplierId, decimal amount, string paymentMode, int userId)
        {
            var supplier = await _db.Suppliers.FindAsync(supplierId);
            if (supplier == null || amount <= 0) return false;

            supplier.PayableBalance = Math.Max(0m, supplier.PayableBalance - amount);

            _db.AuditLogs.Add(new AuditLog
            {
                Action = "SupplierBillPayment",
                TargetEntity = "Supplier",
                Details = $"Paid ₹{amount:N2} to supplier '{supplier.CompanyName}' via {paymentMode}",
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
