using ZXing;
using ZXing.Common;

namespace ShopPro.Core.Services
{
    public class BarcodeService
    {
        /// <summary>
        /// Generates a Code128 or EAN13 barcode pixel matrix using ZXing.Net
        /// </summary>
        public bool[,] GenerateBarcodeMatrix(string content, BarcodeFormat format = BarcodeFormat.CODE_128, int width = 300, int height = 100)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = format,
                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 10,
                    PureBarcode = false
                }
            };

            var pixelData = writer.Write(content);
            var result = new bool[height, width];

            // Convert raw pixel bytes to boolean matrix (black/white)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = (y * width + x) * 4;
                    // Black pixel in RGBA byte array
                    result[y, x] = pixelData.Pixels[offset] == 0;
                }
            }

            return result;
        }
    }
}
