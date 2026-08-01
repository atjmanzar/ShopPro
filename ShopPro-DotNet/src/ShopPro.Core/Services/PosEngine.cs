using ShopPro.Data;
using ShopPro.Data.Entities;
using ShopPro.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    public class PosEngine
    {
        private readonly ShopDbContext _db;

        public List<CartItem> Cart { get; } = new();

        public decimal InvoiceDiscountPercentage { get; set; } = 0.0m;
        public decimal InvoiceFixedDiscount { get; set; } = 0.0m;

        public decimal LineSubtotal => Cart.Sum(item => item.NetSubtotal);
        public decimal LineDiscount => Cart.Sum(item => item.DiscountAmount);

        public decimal InvoiceDiscountAmount
        {
            get
            {
                if (InvoiceDiscountPercentage > 0)
                    return Math.Round(LineSubtotal * (InvoiceDiscountPercentage / 100m), 2);
                return Math.Min(InvoiceFixedDiscount, LineSubtotal);
            }
        }

        public decimal NetSubtotalAfterInvoiceDiscount => Math.Max(0m, LineSubtotal - InvoiceDiscountAmount);
        public decimal TotalTax => Cart.Sum(item => item.TaxAmount);
        public decimal GrandTotal => NetSubtotalAfterInvoiceDiscount + TotalTax;
        public decimal TotalDiscount => LineDiscount + InvoiceDiscountAmount;

        public PosEngine(ShopDbContext db)
        {
            _db = db;
        }

        public async Task<bool> AddProductByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return false;

            var product = await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => (p.Barcode == barcode || p.Sku == barcode) && p.IsActive);

            if (product == null) return false;

            var existing = Cart.FirstOrDefault(i => i.Product.Id == product.Id);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                Cart.Add(new CartItem
                {
                    Product = product,
                    Quantity = 1,
                    UnitPrice = product.Price,
                    TaxRate = product.TaxRate
                });
            }

            return true;
        }

        public void SetPriceOverride(int productId, decimal overridePrice)
        {
            var item = Cart.FirstOrDefault(i => i.Product.Id == productId);
            if (item != null && overridePrice >= 0)
            {
                item.PriceOverride = overridePrice;
            }
        }

        public void RemoveItem(int productId)
        {
            Cart.RemoveAll(i => i.Product.Id == productId);
        }

        public void UpdateQuantity(int productId, int newQuantity)
        {
            var item = Cart.FirstOrDefault(i => i.Product.Id == productId);
            if (item == null) return;

            if (newQuantity <= 0)
                Cart.Remove(item);
            else
                item.Quantity = newQuantity;
        }

        public void ClearCart()
        {
            Cart.Clear();
            InvoiceDiscountPercentage = 0.0m;
            InvoiceFixedDiscount = 0.0m;
        }

        /// <summary>
        /// Single Payment Checkout
        /// </summary>
        public async Task<Sale?> ProcessCheckoutAsync(int userId, PaymentMethod method, decimal amountPaid, int? customerId = null)
        {
            var payments = new List<Payment>
            {
                new Payment
                {
                    Method = method,
                    Amount = amountPaid,
                    PaymentDate = DateTime.UtcNow,
                    ReferenceCode = method == PaymentMethod.Cash ? "CASH-PAID" : $"{method.ToString().ToUpper()}-AUTH"
                }
            };

            return await ProcessSplitCheckoutAsync(userId, payments, customerId);
        }

        /// <summary>
        /// Multi-Payment / Split Payment Checkout
        /// </summary>
        public async Task<Sale?> ProcessSplitCheckoutAsync(int userId, List<Payment> payments, int? customerId = null)
        {
            var totalPaid = payments.Sum(p => p.Amount);
            if (Cart.Count == 0 || totalPaid < GrandTotal) return null;

            var invoiceNum = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";

            var sale = new Sale
            {
                InvoiceNumber = invoiceNum,
                UserId = userId,
                CustomerId = customerId,
                Subtotal = NetSubtotalAfterInvoiceDiscount,
                TotalDiscount = TotalDiscount,
                TotalTax = TotalTax,
                GrandTotal = GrandTotal,
                SaleDate = DateTime.UtcNow,
                Payments = payments
            };

            foreach (var cartItem in Cart)
            {
                sale.Items.Add(new SaleItem
                {
                    ProductId = cartItem.Product.Id,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.EffectivePrice,
                    DiscountAmount = cartItem.DiscountAmount,
                    TaxRate = cartItem.TaxRate,
                    TaxAmount = cartItem.TaxAmount,
                    LineTotal = cartItem.LineTotal
                });

                // Deduct stock in DB and record transaction
                var dbProduct = await _db.Products.FindAsync(cartItem.Product.Id);
                if (dbProduct != null)
                {
                    dbProduct.StockQuantity -= cartItem.Quantity;

                    _db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = dbProduct.Id,
                        QuantityChange = -cartItem.Quantity,
                        Type = TransactionType.SaleDeduction,
                        Reason = $"Sale Invoice #{invoiceNum}",
                        UserId = userId,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();

            ClearCart();
            return sale;
        }

        /// <summary>
        /// Void / Cancel completed sale and restock inventory
        /// </summary>
        public async Task<bool> VoidSaleAsync(int saleId, int userId, string voidReason)
        {
            var sale = await _db.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null) return false;

            foreach (var item in sale.Items)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity; // Restock

                    _db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = product.Id,
                        QuantityChange = item.Quantity,
                        Type = TransactionType.ReturnRestock,
                        Reason = $"Void Sale #{sale.InvoiceNumber}: {voidReason}",
                        UserId = userId,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            _db.Sales.Remove(sale);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
