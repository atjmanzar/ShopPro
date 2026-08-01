using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Hardware;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class HardwareSettingsView : UserControl
    {
        private readonly ShopDbContext _db;
        private readonly HardwareSettingsService _settingsService;

        public HardwareSettingsView()
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _settingsService = new HardwareSettingsService(_db);

            LoadHardwareSettings();
        }

        private async void LoadHardwareSettings()
        {
            var config = await _settingsService.GetHardwareConfigAsync();
            TxtPrinterName.Text = config.ThermalPrinterName;
            CmbPaperWidth.SelectedIndex = config.PaperWidth == PaperWidth.mm58 ? 1 : 0;
            ChkCashDrawer.IsChecked = config.AutoKickCashDrawer;
            TxtVfdCom.Text = config.VfdComPort;
            TxtScaleCom.Text = config.ScaleComPort;
            TxtGstin.Text = config.Gstin;
            TxtFooter.Text = config.FooterMessage;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var config = new HardwareConfig
            {
                ThermalPrinterName = TxtPrinterName.Text.Trim(),
                PaperWidth = CmbPaperWidth.SelectedIndex == 1 ? PaperWidth.mm58 : PaperWidth.mm80,
                AutoKickCashDrawer = ChkCashDrawer.IsChecked ?? true,
                VfdComPort = TxtVfdCom.Text.Trim(),
                ScaleComPort = TxtScaleCom.Text.Trim(),
                Gstin = TxtGstin.Text.Trim(),
                FooterMessage = TxtFooter.Text.Trim()
            };

            await _settingsService.SaveHardwareConfigAsync(config);
            MessageBox.Show("Hardware and peripheral settings saved successfully.", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
