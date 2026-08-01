using ShopPro.Data.Entities;
using ShopPro.UI.Views;
using System.Windows;

namespace ShopPro.UI
{
    public partial class MainWindow : Window
    {
        public User CurrentUser { get; }

        public MainWindow(User user)
        {
            InitializeComponent();
            CurrentUser = user;

            TxtUserFullName.Text = CurrentUser.FullName;
            TxtUserRole.Text = CurrentUser.Role.ToString().ToUpper();

            // Role-Based Visibility (Cashiers only see POS Checkout)
            if (CurrentUser.Role == UserRole.Cashier)
            {
                BtnNavInventory.Visibility = Visibility.Collapsed;
                BtnNavBarcode.Visibility = Visibility.Collapsed;
                BtnNavReports.Visibility = Visibility.Collapsed;
            }

            // Default screen
            NavigateToPos();
        }

        private void BtnNavPos_Click(object sender, RoutedEventArgs e) => NavigateToPos();
        private void BtnNavInventory_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new InventoryView(CurrentUser));
        private void BtnNavBarcode_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new BarcodeGeneratorView());
        private void BtnNavReports_Click(object sender, RoutedEventArgs e) => MainFrame.Navigate(new ReportsView());

        private void NavigateToPos()
        {
            MainFrame.Navigate(new PosCheckoutView(CurrentUser));
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            Close();
        }
    }
}
