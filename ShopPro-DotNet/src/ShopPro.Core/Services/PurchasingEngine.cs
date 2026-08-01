using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class PurchasingEngine
    {
        private readonly ShopDbContext _db;

        public PurchasingEngine(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<List<PurchaseOrder>> GetAllPurchaseOrdersAsync()
        {
            return await _db.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Items)
                .ThenInclude(i => i.Product)
                .OrderByDescending(p => p.OrderDate)
                .ToListAsync();
        }

        public async Task<PurchaseOrder> CreatePurchaseOrderAsync(int supplierId, List<(int productId, int qty, decimal unitCost)> items, string notes)
        {
            var poNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
            var po = new PurchaseOrder
            {
                PoNumber = poNumber,
                SupplierId = supplierId,
                Status = PurchaseOrderStatus.Sent,
                Notes = notes,
                OrderDate = DateTime.UtcNow
            };

            decimal total = 0;
            foreach (var (productId, qty, unitCost) in items)
            {
                var poItem = new PurchaseOrderItem
                {
                    ProductId = productId,
                    QuantityOrdered = qty,
                    UnitCost = unitCost
                };
                po.Items.Add(poItem);
                total += qty * unitCost;
            }

            po.TotalAmount = total;
            _db.PurchaseOrders.Add(po);

            // Increase supplier payable balance
            var supplier = await _db.Suppliers.FindAsync(supplierId);
            if (supplier != null)
            {
                supplier.PayableBalance += total;
            }

            await _db.SaveChangesAsync();
            return po;
        }

        /// <summary>
        /// Goods Received Note (GRN) — Automatically increments product stock in DB and updates cost price
        /// </summary>
        public async Task<bool> ProcessGrnReceiptAsync(int purchaseOrderId, int userId)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);

            if (po == null || po.Status == PurchaseOrderStatus.Received) return false;

            po.Status = PurchaseOrderStatus.Received;
            po.ReceivedDate = DateTime.UtcNow;

            foreach (var item in po.Items)
            {
                item.QuantityReceived = item.QuantityOrdered;

                var product = await _db.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.QuantityOrdered; // Auto Stock Increment
                    product.Cost = item.UnitCost; // Update latest cost price
                    product.UpdatedAt = DateTime.UtcNow;

                    _db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = product.Id,
                        QuantityChange = item.QuantityOrdered,
                        Type = TransactionType.StockIn,
                        Reason = $"GRN Stock Receipt for PO #{po.PoNumber}",
                        UserId = userId,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
