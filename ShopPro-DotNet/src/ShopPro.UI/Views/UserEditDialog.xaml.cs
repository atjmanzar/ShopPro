using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class UserEditDialog : Window
    {
        private readonly ShopDbContext _db;
        private readonly UserManagementService _userService;

        public UserEditDialog()
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _userService = new UserManagementService(_db);
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var username = TxtUsername.Text.Trim();
            var fullName = TxtFullName.Text.Trim();
            var password = TxtPassword.Password.Trim();

            var role = CmbRole.SelectedIndex switch
            {
                1 => UserRole.Manager,
                2 => UserRole.Admin,
                _ => UserRole.Cashier
            };

            var result = await _userService.CreateUserAsync(
                username, password, fullName, role,
                ChkManageProducts.IsChecked ?? false,
                ChkGiveDiscount.IsChecked ?? false,
                ChkViewReports.IsChecked ?? false,
                ChkVoidSale.IsChecked ?? false
            );

            if (result.Success)
            {
                MessageBox.Show(result.Message, "User Created", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            else
            {
                MessageBox.Show(result.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
