namespace ShopPro.Hardware
{
    public class VfdCustomerDisplay
    {
        public string Line1Text { get; private set; } = string.Empty;
        public string Line2Text { get; private set; } = string.Empty;

        public void ClearDisplay()
        {
            Line1Text = string.Empty;
            Line2Text = string.Empty;
        }

        public void DisplayWelcomeMessage(string storeName)
        {
            Line1Text = "WELCOME TO";
            Line2Text = storeName.Length > 20 ? storeName.Substring(0, 20) : storeName;
        }

        public void DisplayItemScanned(string itemName, decimal price)
        {
            Line1Text = itemName.Length > 20 ? itemName.Substring(0, 20) : itemName;
            Line2Text = $"Price: ₹{price:F2}";
        }

        public void DisplayTotal(decimal grandTotal)
        {
            Line1Text = "TOTAL DUE:";
            Line2Text = $"₹{grandTotal:F2}";
        }
    }
}
