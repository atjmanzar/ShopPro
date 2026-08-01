using ShopPro.Core.Services;
using ShopPro.Core.Models;
using ShopPro.Data;
using ShopPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ShopPro.Tests
{
    public class CheckoutWorkflowTests
    {
        private ShopDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<ShopDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            var db = new ShopDbContext(options);
            db.Database.OpenConnection();
            db.Database.EnsureCreated();

            DbInitializer.Initialize(db);
            return db;
        }

        [Fact]
        public void MultiItemCart_MixedTaxRates_CalculatesCorrectTotals()
        {
            // Hand Calculation:
            // Product A: 2x ₹500.00 = ₹1000.00 subtotal. Tax Rate = 18%.
            //            Line A Tax = 1000.00 * 0.18 = ₹180.00. Line A Total = ₹1180.00.
            // Product B: 1x ₹400.00 = ₹400.00 subtotal. Tax Rate = 12%.
            //            Line B Tax = 400.00 * 0.12 = ₹48.00. Line B Total = ₹448.00.
            // Sum of Line Subtotals = 1000.00 + 400.00 = ₹1400.00.
            // Sum of Line Taxes = 180.00 + 48.00 = ₹228.00.
            // Grand Total = 1400.00 + 228.00 = ₹1628.00.

            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);

            var prodA = new Product { Id = 101, Name = "Prod A", Price = 500.00m, TaxRate = 18.00m, StockQuantity = 50 };
            var prodB = new Product { Id = 102, Name = "Prod B", Price = 400.00m, TaxRate = 12.00m, StockQuantity = 50 };

            pos.Cart.Add(new CartItem { Product = prodA, Quantity = 2, UnitPrice = 500.00m, TaxRate = 18.00m });
            pos.Cart.Add(new CartItem { Product = prodB, Quantity = 1, UnitPrice = 400.00m, TaxRate = 12.00m });

            Assert.Equal(1400.00m, pos.LineSubtotal);
            Assert.Equal(228.00m, pos.TotalTax);
            Assert.Equal(1628.00m, pos.GrandTotal);
        }

        [Fact]
        public void LineDiscount_And_InvoiceDiscount_AppliedTogether_ProducesExpectedOrderOfOperations()
        {
            // Hand Calculation Order of Operations:
            // Item 1 (18% Tax): 2x ₹500.00 = ₹1000.00 gross. 10% Line Discount = ₹100.00.
            //                  Net Line Subtotal = 1000.00 - 100.00 = ₹900.00.
            //                  Line Tax = 900.00 * 0.18 = ₹162.00.
            // Pre-tax LineSubtotal Sum = ₹900.00.
            // Invoice Fixed Discount = ₹100.00 pre-tax => Net Pre-tax Subtotal = 900.00 - 100.00 = ₹800.00.
            // Total Tax = ₹162.00.
            // Grand Total = 800.00 + 162.00 = ₹962.00.
            // Total Discount = Line Discount (100.00) + Invoice Discount (100.00) = ₹200.00.

            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);

            var prod = new Product { Id = 103, Name = "Prod C", Price = 500.00m, TaxRate = 18.00m, StockQuantity = 50 };
            pos.Cart.Add(new CartItem
            {
                Product = prod,
                Quantity = 2,
                UnitPrice = 500.00m,
                DiscountPercentage = 10.00m, // 10% line discount
                TaxRate = 18.00m
            });

            pos.InvoiceFixedDiscount = 100.00m; // ₹100 fixed invoice discount

            Assert.Equal(900.00m, pos.LineSubtotal);
            Assert.Equal(100.00m, pos.LineDiscount);
            Assert.Equal(100.00m, pos.InvoiceDiscountAmount);
            Assert.Equal(800.00m, pos.NetSubtotalAfterInvoiceDiscount);
            Assert.Equal(162.00m, pos.TotalTax);
            Assert.Equal(962.00m, pos.GrandTotal);
            Assert.Equal(200.00m, pos.TotalDiscount);
        }

        [Fact]
        public async Task SplitPayment_MatchingGrandTotal_Succeeds()
        {
            // Hand Calculation:
            // Product: 2x ₹500.00 = ₹1000.00 + 18% Tax (₹180.00) = ₹1180.00 Grand Total.
            // Payment 1: Card ₹600.00.
            // Payment 2: UPI ₹580.00.
            // Total Paid = 600.00 + 580.00 = ₹1180.00 == Grand Total. Checkout MUST succeed.

            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);

            var prod = await db.Products.FirstAsync();
            pos.Cart.Add(new CartItem { Product = prod, Quantity = 2, UnitPrice = 500.00m, TaxRate = 18.00m });

            Assert.Equal(1180.00m, pos.GrandTotal);

            var payments = new List<Payment>
            {
                new Payment { Method = PaymentMethod.Card, Amount = 600.00m },
                new Payment { Method = PaymentMethod.Upi, Amount = 580.00m }
            };

            var sale = await pos.ProcessSplitCheckoutAsync(1, payments);

            Assert.NotNull(sale);
            Assert.Equal(1180.00m, sale.GrandTotal);
            Assert.Equal(2, sale.Payments.Count);
        }

        [Fact]
        public async Task SplitPayment_NotAddingUpToGrandTotal_Fails()
        {
            // Hand Calculation:
            // Product: 2x ₹500.00 = ₹1000.00 + 18% Tax (₹180.00) = ₹1180.00 Grand Total.
            // Payment 1: Card ₹600.00.
            // Payment 2: UPI ₹500.00.
            // Total Paid = 600.00 + 500.00 = ₹1100.00 (Short by ₹80.00). Checkout MUST return null (fail).

            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);

            var prod = await db.Products.FirstAsync();
            pos.Cart.Add(new CartItem { Product = prod, Quantity = 2, UnitPrice = 500.00m, TaxRate = 18.00m });

            Assert.Equal(1180.00m, pos.GrandTotal);

            var splitMismatchPayments = new List<Payment>
            {
                new Payment { Method = PaymentMethod.Card, Amount = 600.00m },
                new Payment { Method = PaymentMethod.Upi, Amount = 500.00m } // Total 1100 != 1180
            };

            var sale = await pos.ProcessSplitCheckoutAsync(1, splitMismatchPayments);

            Assert.Null(sale); // Failed due to split payment total mismatch
        }

        [Fact]
        public async Task VoidSale_RestoresProductStockInDatabase_AndLogsInventoryTransaction()
        {
            // Hand Calculation:
            // Maggi Noodles initial stock in DbInitializer = 120 units.
            // Step 1: Buy 2 units => DB stock decreases: 120 - 2 = 118 units.
            // Step 2: Void Sale => Restocks 2 units.
            // Verifies: Directly querying db.Products.FindAsync() returns 120 units.
            // Verifies: InventoryTransaction entry logged with Type = ReturnRestock.

            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);

            var maggi = await db.Products.FirstAsync(p => p.Sku == "SKU-MAGGI-70G");
            int initialStock = maggi.StockQuantity; // 120

            await pos.AddProductByBarcodeAsync("8901234567890"); // Maggi barcode
            pos.UpdateQuantity(maggi.Id, 2); // 2 units

            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 1000.00m);
            Assert.NotNull(sale);

            // Verify post-checkout stock in DB
            var dbPostCheckout = await db.Products.FindAsync(maggi.Id);
            Assert.Equal(initialStock - 2, dbPostCheckout!.StockQuantity); // 118 units

            // Act: Void Sale
            var voidSuccess = await pos.VoidSaleAsync(sale.Id, 1, "Customer canceled order");

            // Assert: Verify DB product stock restored
            Assert.True(voidSuccess);
            var dbPostVoid = await db.Products.FindAsync(maggi.Id);
            Assert.Equal(initialStock, dbPostVoid!.StockQuantity); // Restored to 120 units

            // Assert: Verify InventoryTransaction audit log created
            var transaction = await db.InventoryTransactions
                .FirstOrDefaultAsync(t => t.ProductId == maggi.Id && t.Type == TransactionType.ReturnRestock);
            Assert.NotNull(transaction);
            Assert.Equal(2, transaction.QuantityChange);
            Assert.Contains("Void Sale", transaction.Reason);
        }

        [Fact]
        public void DiscountOver100Percent_AndExcessiveFixedDiscount_FloorsNetPriceAtZeroNotNegative()
        {
            // Hand Calculation:
            // Item 1: Price ₹100.00, Quantity 1. 150% Line Discount applied => Clamped to 100%. Net = ₹0.00.
            // Item 2: Price ₹200.00, Quantity 1. ₹300.00 Fixed Discount applied => Capped at ₹200.00. Net = ₹0.00.
            // Total Subtotal = ₹0.00, Total Tax = ₹0.00, Grand Total = ₹0.00 (Never Negative).

            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);

            var prod1 = new Product { Id = 201, Name = "Item 1", Price = 100.00m, TaxRate = 18.00m, StockQuantity = 10 };
            var prod2 = new Product { Id = 202, Name = "Item 2", Price = 200.00m, TaxRate = 18.00m, StockQuantity = 10 };

            pos.Cart.Add(new CartItem { Product = prod1, Quantity = 1, UnitPrice = 100.00m, DiscountPercentage = 150.00m, TaxRate = 18.00m });
            pos.Cart.Add(new CartItem { Product = prod2, Quantity = 1, UnitPrice = 200.00m, FixedDiscount = 300.00m, TaxRate = 18.00m });

            Assert.Equal(0.00m, pos.Cart[0].NetSubtotal);
            Assert.Equal(0.00m, pos.Cart[1].NetSubtotal);
            Assert.Equal(0.00m, pos.LineSubtotal);
            Assert.Equal(0.00m, pos.TotalTax);
            Assert.Equal(0.00m, pos.GrandTotal);
        }
    }
}
