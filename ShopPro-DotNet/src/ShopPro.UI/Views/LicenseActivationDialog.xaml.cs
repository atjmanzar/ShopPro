using ShopPro.Core.Services;
using ShopPro.Data;
using System.Windows;

namespace ShopPro.UI.Views
{
    public partial class LicenseActivationDialog : Window
    {
        private readonly ShopDbContext _db;
        private readonly LicenseValidationEngine _licenseEngine;

        public LicenseActivationDialog()
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _licenseEngine = new LicenseValidationEngine(_db);

            TxtMachineId.Text = HardwareFingerprintGenerator.GenerateMachineFingerprint();
        }

        private async void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            var key = TxtLicenseKey.Text.Trim();
            var customer = TxtCustomerName.Text.Trim();

            var result = await _licenseEngine.ActivateLicenseKeyAsync(key, customer);
            if (result.Success)
            {
                MessageBox.Show(result.Message, "License Activated", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            else
            {
                MessageBox.Show(result.Message, "Activation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
