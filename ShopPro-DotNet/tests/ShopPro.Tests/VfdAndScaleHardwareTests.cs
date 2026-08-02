using ShopPro.Hardware;
using Xunit;

namespace ShopPro.Tests
{
    public class VfdAndScaleHardwareTests
    {
        [Fact]
        public void SerialScale_ConnectFailure_ReturnsFalseAndSetsLastError()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = false };
            using var scale = new SerialWeighingScale(fakePort);

            var result = scale.Connect("COM1");

            Assert.False(result.Success);
            Assert.False(scale.IsConnected);
            Assert.Contains("Failed to open serial port", result.Message);
        }

        [Fact]
        public void SerialScale_ReadWeightTimeout_ReturnsZeroAndSetsLastError()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = true, SimulateTimeout = true };
            using var scale = new SerialWeighingScale(fakePort);

            scale.Connect("COM1");
            var weight = scale.ReadWeightKg();

            Assert.Equal(0.000m, weight);
            Assert.Contains("timed out", scale.LastError);
        }

        [Fact]
        public void SerialScale_MalformedPacket_ReturnsZero()
        {
            var fakePort = new FakeSerialPortDevice
            {
                SimulateOpenSuccess = true,
                SimulatedReadResponse = "ERR_SCALE_OVERLOAD"
            };
            using var scale = new SerialWeighingScale(fakePort);

            scale.Connect("COM1");
            var weight = scale.ReadWeightKg();

            Assert.Equal(0.000m, weight);
            Assert.Equal("W\r", fakePort.LastWrittenText); // Verified 'W\r' poll command sent
        }

        [Fact]
        public void SerialScale_ValidAsciiPacket_ParsesWeightAndTransmitsPollCommand()
        {
            var fakePort = new FakeSerialPortDevice
            {
                SimulateOpenSuccess = true,
                SimulatedReadResponse = "\x02ST,GS,+02.450kg\x03\r\n" // Toledo scale ASCII packet
            };
            using var scale = new SerialWeighingScale(fakePort);

            scale.Connect("COM1");
            var weight = scale.ReadWeightKg();

            Assert.Equal(2.450m, weight);
            Assert.Equal("W\r", fakePort.LastWrittenText); // Verified 'W\r' poll command sent
        }

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
        public void VfdDisplay_SendToDisplay_TransmitsExactByteStreamToDevice()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = true, SimulateWriteSuccess = true };
            var vfd = new VfdCustomerDisplay(fakePort);

            vfd.DisplayWelcomeMessage("SuperMart POS");
            var result = vfd.SendToDisplay("COM1");

            Assert.True(result.Success);
            Assert.Equal("COM1", fakePort.LastPortName);
            Assert.NotNull(fakePort.LastWrittenBytes);
            Assert.Equal(0x0C, fakePort.LastWrittenBytes[0]); // Clear screen command
        }

        [Fact]
        public void VfdDisplay_SendToDisplayFailure_HandlesSerialPortException()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = false };
            var vfd = new VfdCustomerDisplay(fakePort);

            vfd.DisplayTotal(500.00m);
            var result = vfd.SendToDisplay("COM1");

            Assert.False(result.Success);
            Assert.Contains("VFD serial error", result.Message);
        }

        [Fact(Skip = "Hardware-only verification: requires physical RS-232 Toledo/NCI weighing scale attached via COM port")]
        public void SerialWeighingScale_ReadPhysicalScale_ReadsWeightFromPort()
        {
            using var scale = new SerialWeighingScale();
            scale.Connect("COM2");
            var weight = scale.ReadWeightKg();
            Assert.True(weight >= 0m);
        }

        [Fact(Skip = "Hardware-only verification: requires physical VFD 2x20 customer pole display attached via COM port")]
        public void VfdDisplay_PhysicalHardware_DisplaysTextOnPoleDisplay()
        {
            var vfd = new VfdCustomerDisplay();
            var result = vfd.SendToDisplay("COM1");
            Assert.True(result.Success);
        }
    }
}
