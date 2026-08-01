using ShopPro.Data;
using ShopPro.Data.Entities;
using ShopPro.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ShopPro.Core.Services
{
    public class HeldSaleService
    {
        private readonly ShopDbContext _db;

        public HeldSaleService(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<HeldSale> HoldCartAsync(int userId, List<CartItem> cart, string customerName, decimal subtotal, decimal discount, decimal tax, decimal grandTotal)
        {
            var holdRef = $"HOLD-{DateTime.UtcNow:HHmmss}-{Random.Shared.Next(10, 99)}";
            var json = JsonSerializer.Serialize(cart);

            var heldSale = new HeldSale
            {
                HoldReference = holdRef,
                UserId = userId,
                CustomerName = string.IsNullOrWhiteSpace(customerName) ? "Walk-in Customer" : customerName,
                CartJson = json,
                Subtotal = subtotal,
                TotalDiscount = discount,
                TotalTax = tax,
                GrandTotal = grandTotal,
                HeldAt = DateTime.UtcNow
            };

            _db.HeldSales.Add(heldSale);
            await _db.SaveChangesAsync();
            return heldSale;
        }

        public async Task<List<HeldSale>> GetActiveHeldSalesAsync()
        {
            return await _db.HeldSales
                .Include(h => h.User)
                .OrderByDescending(h => h.HeldAt)
                .ToListAsync();
        }

        public async Task<List<CartItem>?> ResumeHeldSaleAsync(int heldSaleId)
        {
            var heldSale = await _db.HeldSales.FindAsync(heldSaleId);
            if (heldSale == null) return null;

            var items = JsonSerializer.Deserialize<List<CartItem>>(heldSale.CartJson);

            _db.HeldSales.Remove(heldSale);
            await _db.SaveChangesAsync();
            return items;
        }
    }
}
