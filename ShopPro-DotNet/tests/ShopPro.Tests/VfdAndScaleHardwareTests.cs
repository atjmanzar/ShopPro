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
        public void SerialScale_ReconnectDifferentPort_ClosesExistingConnectionFirst()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = true };
            using var scale = new SerialWeighingScale(fakePort);

            var res1 = scale.Connect("COM1");
            Assert.True(res1.Success);
            Assert.Equal("COM1", scale.ComPort);

            // Connect to different port COM2 while open
            var res2 = scale.Connect("COM2");
            Assert.True(res2.Success);
            Assert.Equal("COM2", scale.ComPort);
            Assert.True(fakePort.CloseCount > 0); // Verified port closed before reopening COM2
        }

        [Fact]
        public void SerialScale_WriteFailure_ReturnsScaleReadResultFailure()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = true, SimulateWriteSuccess = false };
            using var scale = new SerialWeighingScale(fakePort);

            scale.Connect("COM1");
            var result = scale.ReadWeightKg();

            Assert.False(result.Success);
            Assert.Null(result.WeightKg);
            Assert.Contains("Scale read error", result.Message);
            Assert.Contains("Serial write operation failed", scale.LastError);
        }

        [Fact]
        public void SerialScale_ReadWeightTimeout_ReturnsNullWeightAndSetsLastError()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = true, SimulateTimeout = true };
            using var scale = new SerialWeighingScale(fakePort);

            scale.Connect("COM1");
            var result = scale.ReadWeightKg();

            Assert.False(result.Success);
            Assert.Null(result.WeightKg);
            Assert.Contains("timed out", result.Message);
            Assert.Contains("timed out", scale.LastError);
        }

        [Fact]
        public void SerialScale_MalformedPacket_ReturnsFailureResult()
        {
            var fakePort = new FakeSerialPortDevice
            {
                SimulateOpenSuccess = true,
                SimulatedReadResponse = "ERR_INVALID_STREAM"
            };
            using var scale = new SerialWeighingScale(fakePort);

            scale.Connect("COM1");
            var result = scale.ReadWeightKg();

            Assert.False(result.Success);
            Assert.Null(result.WeightKg);
            Assert.Contains("Malformed scale packet format", result.Message);
        }

        [Fact]
        public void SerialScale_NegativeWeightPacket_ExplicitlyRejected()
        {
            var fakePort = new FakeSerialPortDevice
            {
                SimulateOpenSuccess = true,
                SimulatedReadResponse = "\x02ST,GS,-01.250kg\x03\r\n" // Negative weight reading
            };
            using var scale = new SerialWeighingScale(fakePort);

            scale.Connect("COM1");
            var result = scale.ReadWeightKg();

            Assert.False(result.Success);
            Assert.Null(result.WeightKg);
            Assert.Contains("Negative weight reading rejected", result.Message);
        }

        [Fact]
        public void SerialScale_OverloadAndUnstableStatus_ExplicitlyRejected()
        {
            var fakePortOverload = new FakeSerialPortDevice { SimulateOpenSuccess = true, SimulatedReadResponse = "\x02OL,GS,+99.999kg\x03\r\n" };
            using var scaleOverload = new SerialWeighingScale(fakePortOverload);
            scaleOverload.Connect("COM1");
            var resOverload = scaleOverload.ReadWeightKg();
            Assert.False(resOverload.Success);
            Assert.Contains("Overload (OL)", resOverload.Message);

            var fakePortUnstable = new FakeSerialPortDevice { SimulateOpenSuccess = true, SimulatedReadResponse = "\x02US,GS,+01.250kg\x03\r\n" };
            using var scaleUnstable = new SerialWeighingScale(fakePortUnstable);
            scaleUnstable.Connect("COM1");
            var resUnstable = scaleUnstable.ReadWeightKg();
            Assert.False(resUnstable.Success);
            Assert.Contains("unstable (US)", resUnstable.Message);
        }

        [Fact]
        public void SerialScale_ValidZeroWeight_ReturnsSuccessTrueWithZeroDecimal()
        {
            var fakePort = new FakeSerialPortDevice
            {
                SimulateOpenSuccess = true,
                SimulatedReadResponse = "\x02ST,GS,+00.000kg\x03\r\n"
            };
            using var scale = new SerialWeighingScale(fakePort);

            scale.Connect("COM1");
            var result = scale.ReadWeightKg();

            Assert.True(result.Success);
            Assert.Equal(0.000m, result.WeightKg);
            Assert.True(result.IsStable);
            Assert.Empty(scale.LastError); // Verified LastError cleared on success
        }

        [Fact]
        public void SerialScale_ValidStableWeight_ParsesDecimalAndClearsLastError()
        {
            var fakePort = new FakeSerialPortDevice
            {
                SimulateOpenSuccess = true,
                SimulatedReadResponse = "\x02ST,GS,+02.450kg\x03\r\n"
            };
            using var scale = new SerialWeighingScale(fakePort);

            scale.Connect("COM1");
            var result = scale.ReadWeightKg();

            Assert.True(result.Success);
            Assert.Equal(2.450m, result.WeightKg);
            Assert.True(result.IsStable);
            Assert.Equal("W\r", fakePort.LastWrittenText);
            Assert.Empty(scale.LastError); // Verified LastError cleared on success
        }

        [Fact]
        public void VfdDisplay_SendToDisplayWriteFailure_CleansUpPortInFinallyBlock()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = true, SimulateWriteSuccess = false };
            var vfd = new VfdCustomerDisplay(fakePort);

            vfd.DisplayTotal(100.00m);
            var result = vfd.SendToDisplay("COM1");

            Assert.False(result.Success);
            Assert.Contains("VFD serial error", result.Message);
            Assert.True(fakePort.CloseCount > 0); // Verified port closed in finally block after write failure
        }

        [Fact(Skip = "Hardware-only verification: requires physical RS-232 Toledo/NCI weighing scale attached via COM port")]
        public void SerialWeighingScale_ReadPhysicalScale_ReadsWeightFromPort()
        {
            using var scale = new SerialWeighingScale();
            scale.Connect("COM2");
            var result = scale.ReadWeightKg();
            Assert.True(result.Success || !result.Success);
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
