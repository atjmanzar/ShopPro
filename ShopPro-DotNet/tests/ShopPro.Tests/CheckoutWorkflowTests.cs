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
        public void InvoiceDiscount_ReallocatesProportionallyAndRecalculatesGstTax_OnPostDiscountAmount()
        {
            // Hand Calculation:
            // Item A (18% Tax): 1x ₹1000.00 = ₹1000.00. Tax without invoice discount = 1000 * 0.18 = ₹180.00.
            // Item B (5% Tax):  1x ₹1000.00 = ₹1000.00. Tax without invoice discount = 1000 * 0.05 = ₹50.00.
            // Total Pre-Invoice Subtotal Sum = ₹2000.00. Total Tax without invoice discount = ₹230.00.
            // 
            // Invoice Fixed Discount = ₹200.00 (10% of total pre-tax subtotal).
            // Proportional Allocation (each line has 50% share of ₹2000):
            // Allocated to Item A: ₹200 * (1000 / 2000) = ₹100.00 => Final Taxable A = 1000 - 100 = ₹900.00.
            // Allocated to Item B: ₹200 * (1000 / 2000) = ₹100.00 => Final Taxable B = 1000 - 100 = ₹900.00.
            // 
            // Recalculated GST Taxes:
            // Item A Tax: 900.00 * 0.18 = ₹162.00 (was 180.00).
            // Item B Tax: 900.00 * 0.05 = ₹45.00  (was 50.00).
            // Total Tax WITH Invoice Discount = 162.00 + 45.00 = ₹207.00.
            // Net Subtotal After Invoice Discount = 900.00 + 900.00 = ₹1800.00.
            // Grand Total = 1800.00 + 207.00 = ₹2007.00.

            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);

            var prodA = new Product { Id = 301, Name = "Item 18%", Price = 1000.00m, TaxRate = 18.00m, StockQuantity = 10 };
            var prodB = new Product { Id = 302, Name = "Item 5%", Price = 1000.00m, TaxRate = 5.00m, StockQuantity = 10 };

            pos.Cart.Add(new CartItem { Product = prodA, Quantity = 1, UnitPrice = 1000.00m, TaxRate = 18.00m });
            pos.Cart.Add(new CartItem { Product = prodB, Quantity = 1, UnitPrice = 1000.00m, TaxRate = 5.00m });

            // Apply ₹200 invoice discount
            pos.InvoiceFixedDiscount = 200.00m;

            Assert.Equal(2000.00m, pos.LineSubtotal);
            Assert.Equal(200.00m, pos.InvoiceDiscountAmount);
            Assert.Equal(1800.00m, pos.NetSubtotalAfterInvoiceDiscount);
            Assert.Equal(207.00m, pos.TotalTax); // Recalculated on post-discount amount
            Assert.Equal(2007.00m, pos.GrandTotal);
        }

        [Fact]
        public async Task ProcessCheckout_CashOverpayment_CalculatesChangeDueCorrectly()
        {
            // Hand Calculation:
            // Product: 2x ₹500.00 = ₹1000.00 + 18% Tax (₹180.00) = ₹1180.00 Grand Total.
            // Payment: Cash ₹2000.00.
            // ChangeDue = 2000.00 - 1180.00 = ₹820.00.

            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);

            var prod = await db.Products.FirstAsync();
            pos.Cart.Add(new CartItem { Product = prod, Quantity = 2, UnitPrice = 500.00m, TaxRate = 18.00m });

            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 2000.00m);

            Assert.NotNull(sale);
            Assert.Equal(1180.00m, sale.GrandTotal);
            Assert.Equal(820.00m, sale.ChangeDue);
        }

        [Fact]
        public async Task Underpayment_WithoutExplicitCreditSaleFlag_IsRejected_EvenWithCustomerIdAttached()
        {
            // Hand Calculation:
            // Product: 2x ₹500.00 = ₹1000.00 + 18% Tax (₹180.00) = ₹1180.00 Grand Total.
            // Payment: Cash ₹1000.00 (Short by ₹180.00).
            // CustomerId attached, but isCreditSale = false (default).
            // Underpayment MUST be rejected (returns null).

            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);

            var customer = new Customer { Name = "Loyalty Member John", Phone = "9876543210" };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            var prod = await db.Products.FirstAsync();
            pos.Cart.Add(new CartItem { Product = prod, Quantity = 2, UnitPrice = 500.00m, TaxRate = 18.00m });

            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 1000.00m, customerId: customer.Id, isCreditSale: false);

            Assert.Null(sale); // Underpayment rejected
        }

        [Fact]
        public async Task ExplicitCreditSale_WithCustomerId_AllowsUnderpayment_AndUpdatesCustomerCreditBalance()
        {
            // Hand Calculation:
            // Product: 2x ₹500.00 = ₹1000.00 + 18% Tax (₹180.00) = ₹1180.00 Grand Total.
            // Payment: Cash ₹1000.00.
            // isCreditSale = true, CustomerId = customer.Id.
            // Unpaid Balance = 1180.00 - 1000.00 = ₹180.00.
            // Verifies: Sale completes, and db.Customers.FindAsync(customer.Id).CreditBalance increases by exactly ₹180.00.

            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);

            var customer = new Customer { Name = "Credit Customer Alice", Phone = "9876543211", CreditBalance = 0.00m };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            var prod = await db.Products.FirstAsync();
            pos.Cart.Add(new CartItem { Product = prod, Quantity = 2, UnitPrice = 500.00m, TaxRate = 18.00m });

            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 1000.00m, customerId: customer.Id, isCreditSale: true);

            Assert.NotNull(sale);
            Assert.Equal(1180.00m, sale.GrandTotal);

            // Assert Customer CreditBalance updated in DB
            var dbCustomer = await db.Customers.FindAsync(customer.Id);
            Assert.NotNull(dbCustomer);
            Assert.Equal(180.00m, dbCustomer.CreditBalance);
        }

        [Fact]
        public async Task VoidSale_RestoresProductStockInDatabase_AndPreservesSaleRecordWithVoidedStatus()
        {
            // Hand Calculation:
            // Maggi Noodles initial stock in DbInitializer = 120 units.
            // Step 1: Buy 2 units => DB stock decreases: 120 - 2 = 118 units.
            // Step 2: Void Sale.
            // Verifies: DB Product stock restored to 120 units.
            // Verifies: Sale record STILL EXISTS in DB with Status == SaleStatus.Voided (not deleted).
            // Verifies: InventoryTransaction entry logged with Type = ReturnRestock.

            using var db = CreateInMemoryDb();
            var pos = new PosEngine(db);

            var maggi = await db.Products.FirstAsync(p => p.Sku == "SKU-MAGGI-70G");
            int initialStock = maggi.StockQuantity; // 120

            await pos.AddProductByBarcodeAsync("8901234567890"); // Maggi barcode
            pos.UpdateQuantity(maggi.Id, 2); // 2 units

            var sale = await pos.ProcessCheckoutAsync(1, PaymentMethod.Cash, 1000.00m);
            Assert.NotNull(sale);

            // Act: Void Sale
            var voidSuccess = await pos.VoidSaleAsync(sale.Id, 1, "Customer canceled order");

            // Assert 1: Product stock restored in DB
            Assert.True(voidSuccess);
            var dbPostVoid = await db.Products.FindAsync(maggi.Id);
            Assert.Equal(initialStock, dbPostVoid!.StockQuantity); // Restored to 120 units

            // Assert 2: Sale record preserved in DB with Voided Status
            var dbSale = await db.Sales.FindAsync(sale.Id);
            Assert.NotNull(dbSale); // Sale record preserved
            Assert.Equal(SaleStatus.Voided, dbSale.Status);

            // Assert 3: InventoryTransaction audit log created
            var transaction = await db.InventoryTransactions
                .FirstOrDefaultAsync(t => t.ProductId == maggi.Id && t.Type == TransactionType.ReturnRestock);
            Assert.NotNull(transaction);
            Assert.Equal(2, transaction.QuantityChange);
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
