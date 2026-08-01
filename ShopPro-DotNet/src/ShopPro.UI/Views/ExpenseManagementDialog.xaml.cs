using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using System.Windows;
using System.Windows.Controls;

namespace ShopPro.UI.Views
{
    public partial class ExpenseManagementDialog : Window
    {
        private readonly User _currentUser;
        private readonly ShopDbContext _db;
        private readonly ExpenseService _expenseService;

        public ExpenseManagementDialog(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _db = new ShopDbContext("");
            _expenseService = new ExpenseService(_db);

            LoadExpenses();
        }

        private async void LoadExpenses()
        {
            var expenses = await _expenseService.GetExpensesAsync(DateTime.Today.AddDays(-30), DateTime.Today.AddDays(1));
            DgExpenses.ItemsSource = expenses;
        }

        private async void BtnAddExpense_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtAmount.Text, out var amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid expense amount greater than 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var title = TxtTitle.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Expense title cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var category = (CmbCategory.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "General";

            await _expenseService.AddExpenseAsync(category, title, amount, "", _currentUser.Id);
            MessageBox.Show("Expense record saved.", "Record Saved", MessageBoxButton.OK, MessageBoxImage.Information);

            TxtTitle.Clear();
            TxtAmount.Clear();
            LoadExpenses();
        }
    }
}
