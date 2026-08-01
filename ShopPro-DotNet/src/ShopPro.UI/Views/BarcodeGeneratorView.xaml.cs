using ShopPro.Core.Services;
using ShopPro.Data;
using ShopPro.Data.Entities;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;

namespace ShopPro.UI.Views
{
    public partial class BarcodeGeneratorView : Page
    {
        private readonly ShopDbContext _db;
        private readonly InventoryService _inventoryService;
        private readonly BarcodeService _barcodeService;

        public BarcodeGeneratorView()
        {
            InitializeComponent();
            _db = new ShopDbContext("");
            _inventoryService = new InventoryService(_db);
            _barcodeService = new BarcodeService();

            LoadProducts();
        }

        private async void LoadProducts()
        {
            var products = await _inventoryService.GetAllProductsAsync();
            CmbProducts.ItemsSource = products;
            if (products.Any()) CmbProducts.SelectedIndex = 0;
        }

        private void CmbProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RenderSelectedBarcode();
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            RenderSelectedBarcode();
            MessageBox.Show("Barcode label compiled and rendered in preview.", "Label Rendered", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSavePng_Click(object sender, RoutedEventArgs e)
        {
            RenderSelectedBarcode();

            var selected = CmbProducts.SelectedItem as Product;
            if (selected == null) return;

            var rtb = new RenderTargetBitmap((int)LabelBorder.ActualWidth, (int)LabelBorder.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(LabelBorder);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            var folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(folder, $"Barcode_{selected.Sku}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

            using (var fs = File.Create(filePath))
            {
                encoder.Save(fs);
            }

            MessageBox.Show($"High-resolution barcode label saved to Desktop:\n{filePath}", "PNG Label Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RenderSelectedBarcode()
        {
            var selected = CmbProducts.SelectedItem as Product;
            if (selected == null) return;

            TxtLabelProductName.Text = selected.Name;
            TxtLabelPrice.Text = $"MRP: ₹{selected.Price:N2}";
            TxtLabelBarcode.Text = selected.Barcode;

            var format = CmbFormat.SelectedIndex == 1 ? BarcodeFormat.EAN_13 : BarcodeFormat.CODE_128;
            var matrix = _barcodeService.GenerateBarcodeMatrix(selected.Barcode, format, 280, 80);

            // Render WriteableBitmap in WPF
            int width = matrix.GetLength(1);
            int height = matrix.GetLength(0);
            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            var pixels = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = (y * width + x) * 4;
                    bool isBlack = matrix[y, x];

                    byte color = isBlack ? (byte)0 : (byte)255;
                    pixels[offset] = color;     // Blue
                    pixels[offset + 1] = color; // Green
                    pixels[offset + 2] = color; // Red
                    pixels[offset + 3] = 255;   // Alpha
                }
            }

            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            ImgBarcodePreview.Source = wb;
        }
    }
}
