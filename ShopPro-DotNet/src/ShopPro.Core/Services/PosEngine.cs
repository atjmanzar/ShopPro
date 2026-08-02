using ShopPro.Data;
using ShopPro.Data.Entities;
using ShopPro.Core.Models;
using ShopPro.Hardware;
using Microsoft.EntityFrameworkCore;

namespace ShopPro.Core.Services
{
    /// <summary>
    /// POS Checkout Engine:
    /// Money Math Order of Operations (Indian GST Compliant):
    /// 1. Line Item Subtotal: RawSubtotal = EffectivePrice * Quantity.
    /// 2. Line Item Discount: DiscountAmount = DiscountEngine.CalculateDiscount(RawSubtotal, Value, Type).
    /// 3. Pre-Invoice Line Net: NetSubtotal = RawSubtotal - DiscountAmount (floored at 0.00).
    /// 4. LineSubtotal Sum: Sum of all line NetSubtotals.
    /// 5. Invoice Discount Calculation: InvoiceDiscountAmount calculated on LineSubtotal Sum (Percentage capped at 100%, Fixed capped at LineSubtotal).
    /// 6. Proportional Invoice Discount Allocation: InvoiceDiscountAmount is allocated proportionally to each line item based on its share of LineSubtotal.
    /// 7. Final Line Taxable Amount: FinalTaxableAmount = NetSubtotal - AllocatedInvoiceDiscount (floored at 0.00).
    /// 8. Line Tax Calculation: TaxAmount = FinalTaxableAmount * (TaxRate / 100) rounded per line (2 decimals AwayFromZero). Under GST, tax is charged strictly on the net post-discount amount paid by the customer.
    /// 9. NetSubtotalAfterInvoiceDiscount: Sum of FinalTaxableAmount across all lines.
    /// 10. TotalTax: Sum of Line TaxAmounts across all lines.
    /// 11. GrandTotal: NetSubtotalAfterInvoiceDiscount + TotalTax.
    /// 
    /// Restocking & Audit Preservation Policy:
    /// - Voiding a completed sale reverses 100% of line item stock deductions and logs an InventoryTransaction.
    /// - Voided sales are preserved in the database with Status = SaleStatus.Voided for tax filing and audit compliance.
    /// </summary>
    public class PosEngine
    {
        private readonly ShopDbContext _db;

        public List<CartItem> Cart { get; } = new();

        public decimal InvoiceDiscountPercentage { get; set; } = 0.0m;
        public decimal InvoiceFixedDiscount { get; set; } = 0.0m;
        public bool IsInterStateTax { get; set; } = false;

        public decimal LineSubtotal => Cart.Sum(item => item.NetSubtotal);
        public decimal LineDiscount => Cart.Sum(item => item.DiscountAmount);

        public decimal InvoiceDiscountAmount
        {
            get
            {
                var subtotal = LineSubtotal;
                if (subtotal <= 0) return 0.0m;

                if (InvoiceDiscountPercentage > 0)
                {
                    var clampedPct = Math.Clamp(InvoiceDiscountPercentage, 0m, 100m);
                    return Math.Round(subtotal * (clampedPct / 100m), 2, MidpointRounding.AwayFromZero);
                }
                return Math.Min(Math.Max(0m, InvoiceFixedDiscount), subtotal);
            }
        }

        public void RecalculateCartDiscountsAndTaxes()
        {
            if (Cart.Count == 0) return;

            var subtotal = LineSubtotal;
            var invDiscount = InvoiceDiscountAmount;

            if (subtotal <= 0 || invDiscount <= 0)
            {
                foreach (var item in Cart)
                {
                    item.AllocatedInvoiceDiscount = 0.0m;
                }
                return;
            }

            decimal accumulatedAllocated = 0.0m;
            for (int i = 0; i < Cart.Count; i++)
            {
                var item = Cart[i];
                if (i == Cart.Count - 1)
                {
                    // Last item gets remaining penny difference for exact sum
                    item.AllocatedInvoiceDiscount = invDiscount - accumulatedAllocated;
                }
                else
                {
                    var ratio = item.NetSubtotal / subtotal;
                    var allocated = Math.Round(invDiscount * ratio, 2, MidpointRounding.AwayFromZero);
                    item.AllocatedInvoiceDiscount = allocated;
                    accumulatedAllocated += allocated;
                }
            }
        }

        public decimal NetSubtotalAfterInvoiceDiscount
        {
            get
            {
                RecalculateCartDiscountsAndTaxes();
                return Cart.Sum(item => item.FinalTaxableAmount);
            }
        }

        public decimal TotalTax
        {
            get
            {
                RecalculateCartDiscountsAndTaxes();
                return Cart.Sum(item => item.TaxAmount);
            }
        }

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
                .FirstOrDefaultAsync(p => (p.Barcode == barcode.Trim() || p.Sku == barcode.Trim()) && p.IsActive);

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

            RecalculateCartDiscountsAndTaxes();
            return true;
        }

        public (bool Valid, string Message) ValidateScaleWeight(decimal weightKg, decimal minIncrement = 0.001m, decimal maxPlausible = 50.000m)
        {
            if (weightKg <= 0.000m)
            {
                return (false, "Weight reading must be greater than 0 kg.");
            }
            if (weightKg < minIncrement)
            {
                return (false, $"Weight reading ({weightKg:F3} kg) is below minimum allowable increment ({minIncrement:F3} kg).");
            }
            if (weightKg > maxPlausible)
            {
                return (false, $"Weight reading ({weightKg:F3} kg) exceeds maximum plausible limit ({maxPlausible:F3} kg). Check scale platform.");
            }
            return (true, "Valid scale weight.");
        }

        public void SetPriceOverride(int productId, decimal overridePrice)
        {
            var item = Cart.FirstOrDefault(i => i.Product.Id == productId);
            if (item != null && overridePrice >= 0)
            {
                item.PriceOverride = overridePrice;
                RecalculateCartDiscountsAndTaxes();
            }
        }

        public void RemoveItem(int productId)
        {
            Cart.RemoveAll(i => i.Product.Id == productId);
            RecalculateCartDiscountsAndTaxes();
        }

        public void UpdateQuantity(int productId, int newQuantity)
        {
            var item = Cart.FirstOrDefault(i => i.Product.Id == productId);
            if (item == null) return;

            if (newQuantity <= 0)
                Cart.Remove(item);
            else
                item.Quantity = newQuantity;

            RecalculateCartDiscountsAndTaxes();
        }

        public void ClearCart()
        {
            Cart.Clear();
            InvoiceDiscountPercentage = 0.0m;
            InvoiceFixedDiscount = 0.0m;
            IsInterStateTax = false;
        }

        /// <summary>
        /// Single Payment Checkout Helper
        /// </summary>
        public async Task<Sale?> ProcessCheckoutAsync(int userId, PaymentMethod method, decimal amountPaid, int? customerId = null, bool isCreditSale = false)
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

            return await ProcessSplitCheckoutAsync(userId, payments, customerId, isCreditSale);
        }

        /// <summary>
        /// Split Payment & Multi-Method Checkout Validation:
        /// - Verifies total paid against invoice GrandTotal.
        /// - Rejects underpayments unless isCreditSale == true AND customerId.HasValue == true.
        /// - Rejects split payments that do not add up to GrandTotal for non-cash split checkouts.
        /// - Calculates ChangeDue for cash payments over GrandTotal.
        /// - Updates Customer Credit Balance for remaining unpaid balances on credit sales.
        /// </summary>
        public async Task<Sale?> ProcessSplitCheckoutAsync(int userId, List<Payment> payments, int? customerId = null, bool isCreditSale = false)
        {
            if (Cart.Count == 0) return null;

            RecalculateCartDiscountsAndTaxes();
            var currentGrandTotal = GrandTotal;
            var totalPaid = payments.Sum(p => p.Amount);

            // Validation 1: Rejects underpayment unless explicitly marked as a credit sale tied to a valid registered customer
            if (totalPaid < currentGrandTotal)
            {
                if (!isCreditSale || !customerId.HasValue)
                {
                    return null; // Underpayment rejected
                }
            }

            // Validation 2: For non-cash split payments (Card + UPI), total must match GrandTotal within 0.01 tolerance
            bool isMultiSplitNonCash = payments.Count > 1 && payments.All(p => p.Method != PaymentMethod.Cash);
            if (isMultiSplitNonCash && Math.Abs(totalPaid - currentGrandTotal) > 0.01m)
            {
                return null; // Split payment total mismatch
            }

            // Calculate ChangeDue for cash payments
            decimal changeDue = 0.00m;
            if (totalPaid > currentGrandTotal)
            {
                var cashPayment = payments.FirstOrDefault(p => p.Method == PaymentMethod.Cash);
                if (cashPayment != null || payments.All(p => p.Method == PaymentMethod.Cash))
                {
                    changeDue = Math.Max(0.00m, totalPaid - currentGrandTotal);
                }
            }

            var invoiceNum = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";

            var sale = new Sale
            {
                InvoiceNumber = invoiceNum,
                UserId = userId,
                CustomerId = customerId,
                Subtotal = NetSubtotalAfterInvoiceDiscount,
                TotalDiscount = TotalDiscount,
                TotalTax = TotalTax,
                GrandTotal = currentGrandTotal,
                ChangeDue = changeDue,
                Status = SaleStatus.Completed,
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
                    DiscountAmount = cartItem.DiscountAmount + cartItem.AllocatedInvoiceDiscount,
                    TaxRate = cartItem.TaxRate,
                    TaxAmount = cartItem.TaxAmount,
                    LineTotal = cartItem.LineTotal
                });

                // Deduct stock in DB and log InventoryTransaction
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

            // If explicit credit sale with registered customer, update Customer Credit Balance for remaining unpaid balance
            if (isCreditSale && customerId.HasValue && totalPaid < currentGrandTotal)
            {
                var customer = await _db.Customers.FindAsync(customerId.Value);
                if (customer != null)
                {
                    customer.CreditBalance += (currentGrandTotal - totalPaid);
                }
            }

            _db.Sales.Add(sale);

            // Audit log completed checkout sale
            _db.AuditLogs.Add(new AuditLog
            {
                Action = "CHECKOUT_COMPLETED",
                TargetEntity = "Sale",
                Details = $"Sale completed successfully. Invoice #{invoiceNum}, Grand Total: Rs. {currentGrandTotal:F2}",
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            ClearCart();
            return sale;
        }

        /// <summary>
        /// Decoupled Receipt Printing Hookup:
        /// Invoked downstream AFTER sale completion. A failed receipt print will NOT cause the completed Sale in EF Core SQLite to roll back.
        /// Records an AuditLog entry in database for hardware print attempt.
        /// </summary>
        public async Task<PrintResult> TryPrintCheckoutReceiptAsync(Sale sale, IPrinterService printerService, string printerName = "")
        {
            if (sale == null || printerService == null)
                return new PrintResult { Success = false, Message = "Invalid sale or printer service." };

            var receiptData = MapSaleToReceiptData(sale);
            var result = await printerService.PrintReceiptWithStatusAsync(receiptData, printerName);

            // Audit log print attempt
            _db.AuditLogs.Add(new AuditLog
            {
                Action = "PRINT_RECEIPT_ATTEMPT",
                TargetEntity = "Sale",
                Details = $"Print attempt for Invoice #{sale.InvoiceNumber} to printer '{printerName}': Success={result.Success}, Message='{result.Message}'",
                UserId = sale.UserId,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            return result;
        }

        /// <summary>
        /// Reprint Last Receipt Capability: Reconstructs ReceiptData from SQLite Sale entity by saleId and triggers print.
        /// Records an AuditLog entry in database for audit compliance.
        /// </summary>
        public async Task<PrintResult> ReprintLastReceiptAsync(int saleId, IPrinterService printerService, string printerName = "")
        {
            var sale = await _db.Sales
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .Include(s => s.Payments)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
                return new PrintResult { Success = false, Message = $"Sale ID #{saleId} not found in database." };

            var receiptData = MapSaleToReceiptData(sale);
            var result = await printerService.PrintReceiptWithStatusAsync(receiptData, printerName);

            // Audit log reprint event
            _db.AuditLogs.Add(new AuditLog
            {
                Action = "REPRINT_RECEIPT",
                TargetEntity = "Sale",
                Details = $"Reprint requested for Invoice #{sale.InvoiceNumber} (Sale ID #{saleId}) to printer '{printerName}': Success={result.Success}",
                UserId = sale.UserId,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            return result;
        }

        public ReceiptData MapSaleToReceiptData(Sale sale)
        {
            var totalPaid = sale.Payments.Sum(p => p.Amount);

            // Map CGST / SGST / IGST tax breakdown matching Stage 3 GST Model
            decimal cgst = 0.0m;
            decimal sgst = 0.0m;
            decimal igst = 0.0m;

            if (IsInterStateTax)
            {
                igst = sale.TotalTax;
            }
            else
            {
                cgst = Math.Round(sale.TotalTax / 2.0m, 2, MidpointRounding.AwayFromZero);
                sgst = sale.TotalTax - cgst;
            }

            return new ReceiptData
            {
                InvoiceNumber = sale.InvoiceNumber,
                CashierName = sale.User?.FullName ?? "Cashier",
                TransactionDate = sale.SaleDate,
                Subtotal = sale.Subtotal,
                Discount = sale.TotalDiscount,
                CgstAmount = cgst,
                SgstAmount = sgst,
                IgstAmount = igst,
                Total = sale.GrandTotal,
                AmountPaid = totalPaid,
                ChangeDue = sale.ChangeDue,
                PaymentMethod = string.Join(", ", sale.Payments.Select(p => p.Method.ToString())),
                Items = sale.Items.Select(i => new ReceiptLineItem
                {
                    ItemName = i.Product?.Name ?? $"Item #{i.ProductId}",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList()
            };
        }

        /// <summary>
        /// Void / Cancel completed sale: Restores stock in DB and preserves sale record with Status = Voided.
        /// </summary>
        public async Task<bool> VoidSaleAsync(int saleId, int userId, string voidReason)
        {
            var sale = await _db.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null || sale.Status == SaleStatus.Voided) return false;

            foreach (var item in sale.Items)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity; // Restore stock

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

            sale.Status = SaleStatus.Voided; // Preserve sale record for audit & tax filing

            _db.AuditLogs.Add(new AuditLog
            {
                Action = "VOID_SALE",
                TargetEntity = "Sale",
                Details = $"Voided Sale Invoice #{sale.InvoiceNumber} (ID #{saleId}). Reason: {voidReason}",
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
