using ShopPro.Core.Services;
using ShopPro.Data;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class UserManagementView : UserControl
    {
        private readonly ShopDbContext _db;
        private readonly UserManagementService _userService;

        public UserManagementView()
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _userService = new UserManagementService(_db);

            LoadUsers();
        }

        private async void LoadUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            DgUsers.ItemsSource = users;
        }

        private void BtnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserEditDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadUsers();
            }
        }
    }
}
