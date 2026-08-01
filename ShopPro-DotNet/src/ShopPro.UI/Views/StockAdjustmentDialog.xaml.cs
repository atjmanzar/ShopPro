using System.Windows;

namespace ShopPro.UI.Views
{
    public partial class StockAdjustmentDialog : Window
    {
        public int QuantityChange { get; private set; }
        public string Reason { get; private set; } = string.Empty;

        public StockAdjustmentDialog(string productName)
        {
            InitializeComponent();
            TxtProductName.Text = productName;
            TxtQtyChange.Text = "10";
            TxtReason.Text = "Stock Restock Audit";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtQtyChange.Text, out var change) || change == 0)
            {
                MessageBox.Show("Please enter a non-zero integer quantity change.", "Invalid Quantity", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtReason.Text))
            {
                MessageBox.Show("Please specify a reason for this stock adjustment.", "Reason Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            QuantityChange = change;
            Reason = TxtReason.Text.Trim();
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
