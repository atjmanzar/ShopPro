using ShopPro.Core.Services;
using ShopPro.Data;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class LicensingConsoleView : UserControl
    {
        private readonly ShopDbContext _db;
        private readonly LicenseValidationEngine _licenseEngine;

        public LicensingConsoleView()
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _licenseEngine = new LicenseValidationEngine(_db);

            LoadLicenseStatus();
        }

        private async void LoadLicenseStatus()
        {
            var state = await _licenseEngine.ValidateCurrentLicenseAsync();
            TxtStatus.Text = state.Message;
            TxtCustomer.Text = state.CustomerName;
            TxtTrialDays.Text = state.IsTrial ? $"{state.RemainingTrialDays} Days Remaining" : "Activated (Commercial)";
            TxtMachineFingerprint.Text = state.MachineFingerprint;
        }

        private void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new LicenseActivationDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadLicenseStatus();
            }
        }

        private async void BtnDeactivate_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Are you sure you want to deactivate the current commercial license on this workstation?", "Confirm Deactivation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _licenseEngine.DeactivateCurrentLicenseAsync();
            MessageBox.Show("License deactivated. Workstation returned to trial mode.", "License Deactivated", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadLicenseStatus();
        }
    }
}
