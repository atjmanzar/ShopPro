using ShopPro.Core.Services;
using System.Text;
using System.Windows;

namespace ShopPro.UI.Views
{
    public partial class AutoUpdateDialog : Window
    {
        private readonly UpdateCheckerService _updateService;
        private readonly UpdateCheckResult _updateResult;

        public AutoUpdateDialog(UpdateCheckResult updateResult)
        {
            InitializeComponent();
            _updateService = new UpdateCheckerService();
            _updateResult = updateResult;

            DisplayUpdateInfo();
        }

        private void DisplayUpdateInfo()
        {
            TxtVersionHeading.Text = $"ShopPro v{_updateResult.Manifest.LatestVersion} is available (Installed: v{_updateResult.CurrentVersion})";

            var sb = new StringBuilder();
            foreach (var note in _updateResult.Manifest.ReleaseNotes)
            {
                sb.AppendLine(note);
            }
            TxtReleaseNotes.Text = sb.ToString();
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            BtnInstall.IsEnabled = false;
            PbDownload.Visibility = Visibility.Visible;
            TxtStatus.Text = "Downloading update package...";

            var path = await _updateService.DownloadUpdatePackageAsync(_updateResult.Manifest.DownloadUrl, progress =>
            {
                PbDownload.Value = progress;
            });

            TxtStatus.Text = "Update downloaded. Ready to launch setup installer.";
            MessageBox.Show($"Update package saved to:\n{path}\n\nClick OK to launch installer and update ShopPro.", "Update Ready", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }

        private void BtnLater_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
