using ShopPro.Core.Services;
using ZXing;
using Xunit;

namespace ShopPro.Tests
{
    public class BarcodeServiceTests
    {
        [Fact]
        public void AutoDetectSymbology_13Digits_DetectsEan13()
        {
            var service = new BarcodeService();
            var format = service.AutoDetectSymbology("8901234567890"); // 13 digits
            Assert.Equal(BarcodeFormat.EAN_13, format);
        }

        [Fact]
        public void AutoDetectSymbology_8Digits_DetectsEan8()
        {
            var service = new BarcodeService();
            var format = service.AutoDetectSymbology("12345678"); // 8 digits
            Assert.Equal(BarcodeFormat.EAN_8, format);
        }

        [Fact]
        public void AutoDetectSymbology_AlphanumericSku_DetectsCode128()
        {
            var service = new BarcodeService();
            var format = service.AutoDetectSymbology("SKU-MAGGI-70G");
            Assert.Equal(BarcodeFormat.CODE_128, format);
        }

        [Fact]
        public void GenerateBarcodeMatrix_EmptyString_ThrowsArgumentException()
        {
            var service = new BarcodeService();
            Assert.Throws<ArgumentException>(() => service.GenerateBarcodeMatrix(""));
        }

        [Fact]
        public void GenerateBarcodeMatrix_ValidBarcode_ReturnsBooleanPixelMatrix()
        {
            var service = new BarcodeService();
            var matrix = service.GenerateBarcodeMatrix("8901234567890", width: 300, height: 100);

            Assert.NotNull(matrix);
            Assert.Equal(100, matrix.GetLength(0)); // Height
            Assert.Equal(300, matrix.GetLength(1)); // Width
        }
    }
}
