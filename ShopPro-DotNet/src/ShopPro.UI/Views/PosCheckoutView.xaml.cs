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
        private readonly HardwareSettingsService _hardwareSettingsService;
        private readonly EscPosPrinterService _printerService;
        private readonly KeyboardWedgeScanner _scanner;
        private Sale? _lastCompletedSale;

        public PosCheckoutView(User user)
        {
            InitializeComponent();
            _currentUser = user;
            _db = new ShopDbContext("");
            _posEngine = new PosEngine(_db);
            _heldSaleService = new HeldSaleService(_db);
            _hardwareSettingsService = new HardwareSettingsService(_db);
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

            TxtSubtotal.Text = $"Rs. {_posEngine.LineSubtotal:N2}";
            TxtTotalDiscount.Text = $"Rs. {_posEngine.TotalDiscount:N2}";
            TxtTotalTax.Text = $"Rs. {_posEngine.TotalTax:N2}";
            TxtGrandTotal.Text = $"Rs. {_posEngine.GrandTotal:N2}";

            var taxBreakdown = TaxEngine.CalculateTax(_posEngine.NetSubtotalAfterInvoiceDiscount, 18.00m);
            TxtTaxBreakdown.Text = $"CGST (9%): Rs. {taxBreakdown.CgstAmount:N2} | SGST (9%): Rs. {taxBreakdown.SgstAmount:N2}";

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

            // Step 1: Execute Financial Checkout in Database
            var sale = await _posEngine.ProcessCheckoutAsync(_currentUser.Id, method, paid);
            if (sale != null)
            {
                _lastCompletedSale = sale;
                var change = paid - sale.GrandTotal;

                // Step 2: Load Configured Hardware Settings from SQLite DB
                var hwConfig = await _hardwareSettingsService.GetHardwareConfigAsync();

                // Step 3: Decoupled Receipt Print Attempt
                var printResult = await _posEngine.TryPrintCheckoutReceiptAsync(sale, _printerService, hwConfig.ThermalPrinterName);

                // Step 4: Auto-kick cash drawer if enabled in settings
                if (hwConfig.AutoKickCashDrawer)
                {
                    await _printerService.OpenCashDrawerAsync(hwConfig.ThermalPrinterName);
                }

                // Step 5: Surface Cashier Notification without rolling back sale or freezing UI
                string msg = $"Checkout Completed Successfully!\nInvoice #: {sale.InvoiceNumber}\nChange Due: Rs. {change:N2}";
                if (!printResult.Success)
                {
                    msg += $"\n\nPrinter Status Warning:\n{printResult.Message}\n(Sale is recorded in database. You can reprint the receipt once the printer is ready).";
                    MessageBox.Show(msg, "Checkout Complete (Printer Warning)", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(msg, "Payment & Checkout Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                RefreshCartUi();
            }
        }

        private async void BtnReprintReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (_lastCompletedSale == null)
            {
                MessageBox.Show("No completed sale available to reprint. Perform a checkout first.", "Reprint Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var hwConfig = await _hardwareSettingsService.GetHardwareConfigAsync();
            var result = await _posEngine.ReprintLastReceiptAsync(_lastCompletedSale.Id, _printerService, hwConfig.ThermalPrinterName);

            if (result.Success)
            {
                MessageBox.Show($"Receipt reprinted successfully for Invoice #{_lastCompletedSale.InvoiceNumber}.", "Reprint Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Reprint Status:\n{result.Message}", "Reprint Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnClearCart_Click(object sender, RoutedEventArgs e)
        {
            _posEngine.ClearCart();
            RefreshCartUi();
        }
    }
}
