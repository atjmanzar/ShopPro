using ShopPro.Hardware;
using Xunit;

namespace ShopPro.Tests
{
    public class VfdAndScaleHardwareTests
    {
        [Fact]
        public void VfdDisplay_GenerateSerialBytes_FormatsEscPosVfdProtocolCommandBytes()
        {
            var vfd = new VfdCustomerDisplay();
            vfd.DisplayItemScanned("Maggi 2-Minute Noodles", 48.00m);

            var bytes = vfd.GenerateSerialBytes();

            Assert.NotNull(bytes);
            Assert.Equal(0x0C, bytes[0]); // Form Feed (Clear Screen)
            Assert.Equal(0x1B, bytes[1]); // ESC
            Assert.Equal(0x51, bytes[2]); // Q
            Assert.Equal(0x41, bytes[3]); // A (Line 1 position)
        }

        [Fact]
        public void SerialWeighingScale_ParseWeightPacket_ExtractsNumericWeightFromAsciiStream()
        {
            var scale = new SerialWeighingScale();
            string asciiPacket = "\x02ST,GS,+01.250kg\x03\r\n"; // Toledo scale ASCII packet format

            var weight = scale.ParseWeightPacket(asciiPacket);

            Assert.Equal(1.250m, weight);
        }

        [Fact(Skip = "Requires physical RS-232 serial hardware attached via COM port")]
        public void SerialWeighingScale_ReadPhysicalScale_ReadsWeightFromPort()
        {
            var scale = new SerialWeighingScale();
            scale.Connect("COM2");
            var weight = scale.ReadWeightKg();
            Assert.True(weight >= 0m);
        }
    }
}
