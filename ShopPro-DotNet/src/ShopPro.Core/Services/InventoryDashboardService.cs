using ShopPro.Data;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class InventoryDashboardSummary
    {
        public int TotalProducts { get; set; }
        public decimal ValuationAtCost { get; set; }
        public decimal ValuationAtRetail { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
    }

    public class InventoryDashboardService
    {
        private readonly ShopDbContext _db;

        public InventoryDashboardService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<InventoryDashboardSummary> GetDashboardSummaryAsync()
        {
            var products = await _db.Products
                .Where(p => p.IsActive)
                .ToListAsync();

            return new InventoryDashboardSummary
            {
                TotalProducts = products.Count,
                ValuationAtCost = products.Sum(p => (decimal)p.StockQuantity * p.Cost),
                ValuationAtRetail = products.Sum(p => (decimal)p.StockQuantity * p.Price),
                LowStockCount = products.Count(p => p.StockQuantity <= p.MinStockAlert && p.StockQuantity > 0),
                OutOfStockCount = products.Count(p => p.StockQuantity <= 0)
            };
        }
    }
}
