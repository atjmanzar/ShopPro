using ShopPro.Core.Services;
using ZXing;
using Xunit;

namespace ShopPro.Tests
{
    public class BarcodeServiceTests
    {
        [Fact]
        public void GenerateBarcodeMatrix_Code128_ReturnsNonEmptyMatrix()
        {
            // Arrange
            var barcodeService = new BarcodeService();

            // Act
            var matrix = barcodeService.GenerateBarcodeMatrix("8901234567890", BarcodeFormat.CODE_128, 300, 100);

            // Assert
            Assert.NotNull(matrix);
            Assert.Equal(100, matrix.GetLength(0)); // Height
            Assert.Equal(300, matrix.GetLength(1)); // Width
        }
    }
}
