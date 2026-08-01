using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using ShopPro.Hardware;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShopPro.UI.Views
{
    public partial class PosCheckoutView : Page
    {
        private readonly User _currentUser;
        private readonly ShopDbContext _db;
        private readonly PosEngine _posEngine;
        private readonly HeldSaleService _heldSaleService;
        private readonly EscPosPrinterService _printerService;
        private readonly KeyboardWedgeScanner _scanner;

        public PosCheckoutView(User user)
        {
            InitializeComponent();
            _currentUser = user;
            _db = new ShopDbContext("");
            _posEngine = new PosEngine(_db);
            _heldSaleService = new HeldSaleService(_db);
            _printerService = new EscPosPrinterService();

            _scanner = new KeyboardWedgeScanner();
            _scanner.BarcodeScanned += Scanner_BarcodeScanned;

            // Global Key Listener for Keyboard Wedge Scanner
            Loaded += (s, e) => Window.GetWindow(this)?.KeyDown += Window_KeyDown;
            Unloaded += (s, e) => Window.GetWindow(this)?.KeyDown -= Window_KeyDown;

            RefreshCartUi();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.None && !TxtSearchBarcode.IsFocused)
            {
                var keyChar = (char)KeyInterop.VirtualKeyFromKey(e.Key);
                _scanner.ProcessKeystroke(keyChar);
            }
        }

        private async void Scanner_BarcodeScanned(object? sender, BarcodeScannedEventArgs e)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                var success = await _posEngine.AddProductByBarcodeAsync(e.Barcode);
                if (success)
                {
                    RefreshCartUi();
                }
                else
                {
                    MessageBox.Show($"Product with Barcode '{e.Barcode}' not found.", "Barcode Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
        }

        private async void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var code = TxtSearchBarcode.Text.Trim();
            if (string.IsNullOrEmpty(code)) return;

            var success = await _posEngine.AddProductByBarcodeAsync(code);
            if (success)
            {
                TxtSearchBarcode.Clear();
                RefreshCartUi();
            }
            else
            {
                MessageBox.Show($"Product '{code}' not found.", "Item Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TxtSearchBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnAddProduct_Click(sender, e);
            }
        }

        private void TxtSearchBarcode_GotFocus(object sender, RoutedEventArgs e)
        {
            TxtSearchBarcode.SelectAll();
        }

        private void TxtInvoiceDiscount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(TxtInvoiceDiscountPct.Text, out var pct))
            {
                _posEngine.InvoiceDiscountPercentage = Math.Clamp(pct, 0m, 100m);
            }
            else
            {
                _posEngine.InvoiceDiscountPercentage = 0m;
            }
            RefreshCartUi();
        }

        private async void BtnHoldSale_Click(object sender, RoutedEventArgs e)
        {
            if (!_posEngine.Cart.Any())
            {
                MessageBox.Show("Cart is empty. Add products before holding a sale.", "Empty Cart", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var held = await _heldSaleService.HoldCartAsync(_currentUser.Id, _posEngine.Cart, "Walk-in Customer", _posEngine.LineSubtotal, _posEngine.TotalDiscount, _posEngine.TotalTax, _posEngine.GrandTotal);
            MessageBox.Show($"Sale Cart held successfully.\nHold Reference: {held.HoldReference}", "Sale Cart Held", MessageBoxButton.OK, MessageBoxImage.Information);

            _posEngine.ClearCart();
            RefreshCartUi();
        }

        private void BtnResumeSale_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new HeldSalesDialog();
            if (dialog.ShowDialog() == true && dialog.ResumedCart != null)
            {
                _posEngine.ClearCart();
                _posEngine.Cart.AddRange(dialog.ResumedCart);
                RefreshCartUi();
                MessageBox.Show("Held sale cart resumed into checkout workstation.", "Cart Resumed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RefreshCartUi()
        {
            DgCart.ItemsSource = null;
            DgCart.ItemsSource = _posEngine.Cart;

            TxtSubtotal.Text = $"₹{_posEngine.LineSubtotal:N2}";
            TxtTotalDiscount.Text = $"₹{_posEngine.TotalDiscount:N2}";
            TxtTotalTax.Text = $"₹{_posEngine.TotalTax:N2}";
            TxtGrandTotal.Text = $"₹{_posEngine.GrandTotal:N2}";

            var taxBreakdown = TaxEngine.CalculateTax(_posEngine.NetSubtotalAfterInvoiceDiscount, 18.00m);
            TxtTaxBreakdown.Text = $"CGST (9%): ₹{taxBreakdown.CgstAmount:N2} | SGST (9%): ₹{taxBreakdown.SgstAmount:N2}";

            TxtAmountPaid.Text = _posEngine.GrandTotal.ToString("F2");
        }

        private async void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (!_posEngine.Cart.Any())
            {
                MessageBox.Show("Cart is empty. Add products before checkout.", "Empty Cart", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!decimal.TryParse(TxtAmountPaid.Text, out var paid) || paid < _posEngine.GrandTotal)
            {
                MessageBox.Show("Amount tendered is less than Grand Total.", "Insufficient Payment", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PaymentMethod method = CmbPaymentMethod.SelectedIndex switch
            {
                1 => PaymentMethod.CreditCard,
                2 => PaymentMethod.UPI,
                3 => PaymentMethod.StoreCredit,
                _ => PaymentMethod.Cash
            };

            var sale = await _posEngine.ProcessCheckoutAsync(_currentUser.Id, method, paid);
            if (sale != null)
            {
                var change = paid - sale.GrandTotal;

                // Print ESC/POS Receipt
                var receipt = new ReceiptData
                {
                    InvoiceNumber = sale.InvoiceNumber,
                    CashierName = _currentUser.FullName,
                    Subtotal = sale.Subtotal,
                    Discount = sale.TotalDiscount,
                    Tax = sale.TotalTax,
                    Total = sale.GrandTotal,
                    AmountPaid = paid,
                    ChangeDue = change,
                    PaymentMethod = method.ToString(),
                    Items = sale.Items.Select(i => new ReceiptLineItem
                    {
                        ItemName = i.Product?.Name ?? "Item",
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        LineTotal = i.LineTotal
                    }).ToList()
                };

                await _printerService.PrintReceiptAsync(receipt);
                await _printerService.OpenCashDrawerAsync();

                MessageBox.Show($"Checkout Successful!\nInvoice #: {sale.InvoiceNumber}\nPayment Method: {method}\nChange Due: ₹{change:N2}", "Payment Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshCartUi();
            }
        }

        private void BtnClearCart_Click(object sender, RoutedEventArgs e)
        {
            _posEngine.ClearCart();
            RefreshCartUi();
        }
    }
}
