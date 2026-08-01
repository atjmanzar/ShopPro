using ShopPro.Core.Services;
using ShopPro.Data;
using System.Windows;

namespace ShopPro.UI
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            TxtUsername.Focus();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Text = string.Empty;

            using var db = new ShopDbContext("");
            var authService = new AuthService(db);

            var user = await authService.LoginAsync(TxtUsername.Text, TxtPassword.Password);
            if (user != null)
            {
                var mainWin = new MainWindow(user);
                mainWin.Show();
                Close();
            }
            else
            {
                TxtError.Text = "Invalid username or password. Please try again.";
            }
        }
    }
}
