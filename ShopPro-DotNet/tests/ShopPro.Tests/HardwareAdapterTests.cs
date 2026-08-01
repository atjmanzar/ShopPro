using ShopPro.Hardware;
using Xunit;

namespace ShopPro.Tests
{
    public class HardwareAdapterTests
    {
        [Fact]
        public void VfdCustomerDisplay_DisplaysScannedItemAndTotals()
        {
            // Arrange
            var vfd = new VfdCustomerDisplay();

            // Act
            vfd.DisplayItemScanned("Coca-Cola 750ml PET", 40.00m);

            // Assert
            Assert.Equal("Coca-Cola 750ml PET", vfd.Line1Text);
            Assert.Equal("Price: ₹40.00", vfd.Line2Text);
        }

        [Fact]
        public void SerialWeighingScale_ReadsWeightWhenConnected()
        {
            // Arrange
            var scale = new SerialWeighingScale();
            scale.Connect("COM2");

            // Act
            var weight = scale.ReadWeightKg();

            // Assert
            Assert.True(scale.IsConnected);
            Assert.Equal(1.250m, weight);
        }
    }
}
