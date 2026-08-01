using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class ShiftManagementDialog : Window
    {
        private readonly User _currentUser;
        private readonly ShopDbContext _db;
        private readonly ShiftManagementService _shiftService;
        private CashShift? _activeShift;

        public ShiftManagementDialog(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _db = new ShopDbContext("");
            _shiftService = new ShiftManagementService(_db);

            LoadActiveShift();
        }

        private async void LoadActiveShift()
        {
            _activeShift = await _shiftService.GetActiveShiftAsync(_currentUser.Id);
            if (_activeShift == null)
            {
                _activeShift = await _shiftService.OpenShiftAsync(_currentUser.Id, 2000.00m); // Default ₹2,000 float
            }

            TxtOpeningFloat.Text = $"₹{_activeShift.OpeningFloat:N2}";
            TxtCashSales.Text = $"₹{_activeShift.TotalCashSales:N2}";
            TxtExpectedCash.Text = $"₹{_activeShift.ExpectedCash:N2}";
            TxtCountedCash.Text = _activeShift.ExpectedCash.ToString("F2");
        }

        private void TxtCountedCash_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_activeShift == null) return;

            if (decimal.TryParse(TxtCountedCash.Text, out var counted))
            {
                var variance = counted - _activeShift.ExpectedCash;
                TxtVariance.Text = $"₹{variance:N2}";
            }
        }

        private async void BtnCloseShift_Click(object sender, RoutedEventArgs e)
        {
            if (_activeShift == null) return;

            if (!decimal.TryParse(TxtCountedCash.Text, out var counted))
            {
                MessageBox.Show("Please enter a valid cash count amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var closed = await _shiftService.CloseShiftAsync(_activeShift.Id, counted);
            if (closed != null)
            {
                MessageBox.Show($"Shift Closed & Reconciled Successfully!\nExpected Cash: ₹{closed.ExpectedCash:N2}\nCounted Cash: ₹{closed.ClosingCashCount:N2}\nVariance: ₹{closed.Variance:N2}", "Shift Closed", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
