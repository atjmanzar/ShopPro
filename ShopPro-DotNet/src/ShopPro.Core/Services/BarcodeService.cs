using ZXing;
using ZXing.Common;

namespace ShopPro.Core.Services
{
    public class BarcodeService
    {
        /// <summary>
        /// Auto-detects barcode symbology (EAN-13, EAN-8, CODE-128) and generates a boolean pixel matrix.
        /// Rejects empty or malformed barcodes with ArgumentException.
        /// </summary>
        public bool[,] GenerateBarcodeMatrix(string content, BarcodeFormat? format = null, int width = 300, int height = 100)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Barcode content cannot be empty.", nameof(content));
            }

            var cleanContent = content.Trim();
            var targetFormat = format ?? AutoDetectSymbology(cleanContent);

            var writer = new BarcodeWriterPixelData
            {
                Format = targetFormat,
                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 10,
                    PureBarcode = false
                }
            };

            var pixelData = writer.Write(cleanContent);
            var result = new bool[height, width];

            // Convert raw RGBA pixel bytes to boolean matrix (black/white)
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

        public BarcodeFormat AutoDetectSymbology(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return BarcodeFormat.CODE_128;

            bool isAllDigits = content.All(char.IsDigit);
            if (isAllDigits)
            {
                if (content.Length == 13) return BarcodeFormat.EAN_13;
                if (content.Length == 8) return BarcodeFormat.EAN_8;
            }

            return BarcodeFormat.CODE_128;
        }
    }
}
