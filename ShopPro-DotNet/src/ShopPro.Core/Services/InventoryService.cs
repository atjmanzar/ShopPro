using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class InventoryService
    {
        private readonly ShopDbContext _db;

        public InventoryService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<List<Product>> GetLowStockProductsAsync()
        {
            return await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity <= p.MinStockAlert && p.StockQuantity > 0)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();
        }

        public async Task<List<Product>> GetOutOfStockProductsAsync()
        {
            return await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity <= 0)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<bool> StockInAsync(int productId, int quantity, string supplierReason, int userId)
        {
            if (quantity <= 0) return false;
            return await AdjustStockAsync(productId, quantity, $"Stock In: {supplierReason}", userId, TransactionType.StockIn);
        }

        public async Task<bool> StockOutAsync(int productId, int quantity, string reason, int userId)
        {
            if (quantity <= 0) return false;
            return await AdjustStockAsync(productId, -quantity, $"Stock Out: {reason}", userId, TransactionType.StockOut);
        }

        public async Task<bool> AdjustStockAsync(int productId, int quantityChange, string reason, int userId, TransactionType type = TransactionType.Adjustment)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return false;

            if (product.StockQuantity + quantityChange < 0) return false; // Negative stock protection

            product.StockQuantity += quantityChange;
            product.UpdatedAt = DateTime.UtcNow;

            var transaction = new InventoryTransaction
            {
                ProductId = productId,
                QuantityChange = quantityChange,
                Type = type,
                Reason = reason,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            };

            _db.InventoryTransactions.Add(transaction);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
