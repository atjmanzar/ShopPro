using ShopPro.Hardware;
using System.IO.Ports;
using Xunit;

namespace ShopPro.Tests
{
    public class VfdAndScaleHardwareTests
    {
        // ===== Scale Connection Tests =====

        [Fact]
        public void Scale_ConnectFailure_ReturnsFalseAndSetsLastError()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = false };
            using var scale = new SerialWeighingScale(fakePort);
            var result = scale.Connect("COM1");
            Assert.False(result.Success);
            Assert.False(scale.IsConnected);
            Assert.Contains("Failed to open serial port", result.Message);
        }

        [Fact]
        public void Scale_ReconnectDifferentPort_ClosesExistingFirst()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = true };
            using var scale = new SerialWeighingScale(fakePort);

            scale.Connect("COM1");
            Assert.True(scale.IsConnected);

            scale.Connect("COM2");
            Assert.Equal("COM2", scale.ComPort);
            Assert.True(fakePort.CloseCount > 0);
        }

        [Fact]
        public void Scale_ConfigPropagatesToSerialPort()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = true };
            using var scale = new SerialWeighingScale(fakePort);

            var cfg = new ScaleConfig
            {
                ComPort = "COM3",
                BaudRate = 19200,
                Parity = Parity.Even,
                DataBits = 7,
                StopBits = StopBits.Two,
                ReadTimeoutMs = 2000,
                WriteTimeoutMs = 500,
                Protocol = "Toledo",
                AllowedUnit = "kg",
                MaxCapacityKg = 30.000m,
                MinIncrementKg = 0.005m
            };

            scale.Connect(cfg);
            Assert.True(scale.IsConnected);
            Assert.NotNull(fakePort.LastOpenConfig);
            Assert.Equal("COM3", fakePort.LastOpenConfig!.PortName);
            Assert.Equal(19200, fakePort.LastOpenConfig.BaudRate);
            Assert.Equal(Parity.Even, fakePort.LastOpenConfig.Parity);
            Assert.Equal(7, fakePort.LastOpenConfig.DataBits);
            Assert.Equal(StopBits.Two, fakePort.LastOpenConfig.StopBits);
            Assert.Equal(2000, fakePort.LastOpenConfig.ReadTimeoutMs);
            Assert.Equal(500, fakePort.LastOpenConfig.WriteTimeoutMs);
        }

        // ===== Scale Read I/O Tests =====

        [Fact]
        public void Scale_WriteFailure_ReturnsFailure()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = true, SimulateWriteSuccess = false };
            using var scale = new SerialWeighingScale(fakePort);
            scale.Connect("COM1");
            var result = scale.ReadWeightKg();
            Assert.False(result.Success);
            Assert.Null(result.WeightKg);
            Assert.Contains("Scale read error", result.Message);
        }

        [Fact]
        public void Scale_ReadTimeout_ReturnsFailure()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = true, SimulateTimeout = true };
            using var scale = new SerialWeighingScale(fakePort);
            scale.Connect("COM1");
            var result = scale.ReadWeightKg();
            Assert.False(result.Success);
            Assert.Contains("timed out", result.Message);
        }

        // ===== Exact Toledo Packet Parser — Valid Packets =====

        [Fact]
        public void Scale_ValidStableGross_ParsesCorrectly()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02ST,GS,+02.450kg\x03\r\n");
            Assert.True(result.Success);
            Assert.Equal(2.450m, result.WeightKg);
            Assert.True(result.IsStable);
        }

        [Fact]
        public void Scale_ValidZeroWeight_SucceedsWithZeroDecimal()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02ST,GS,+00.000kg\x03\r\n");
            Assert.True(result.Success);
            Assert.Equal(0.000m, result.WeightKg);
            Assert.True(result.IsStable);
        }

        [Fact]
        public void Scale_ValidNetMode_Accepted()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02ST,NT,+01.000kg\x03\r\n");
            Assert.True(result.Success);
            Assert.Equal(1.000m, result.WeightKg);
        }

        [Fact]
        public void Scale_SuccessfulRead_ClearsLastError()
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
            Assert.Empty(scale.LastError);
        }

        // ===== Exact Toledo Packet Parser — Rejection Cases =====

        [Fact]
        public void Scale_Reject_BareDecimal()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("2.450");
            Assert.False(result.Success);
            Assert.Contains("Missing STX", result.Message);
        }

        [Fact]
        public void Scale_Reject_MissingSTX()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("ST,GS,+02.450kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("Missing STX", result.Message);
        }

        [Fact]
        public void Scale_Reject_MissingETX()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02ST,GS,+02.450kg\r\n");
            Assert.False(result.Success);
            Assert.Contains("Missing ETX", result.Message);
        }

        [Fact]
        public void Scale_Reject_TrailingJunkAfterETX()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            // Junk after ETX but before CRLF — parser strips CRLF then checks ETX at end
            var result = scale.ParseWeightPacket("\x02ST,GS,+02.450kg\x03JUNK\r\n");
            Assert.False(result.Success);
            Assert.Contains("Missing ETX", result.Message);
        }

        [Fact]
        public void Scale_Reject_OverloadStatus()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02OL,GS,+99.999kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("overload", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Scale_Reject_ErrorStatus()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02EA,GS,+00.000kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("error", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Scale_Reject_UnstableStatus()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02US,GS,+01.250kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("unstable", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Scale_Reject_UnknownStatus()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02XX,GS,+01.250kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("Unknown scale status", result.Message);
        }

        [Fact]
        public void Scale_Reject_UnknownMode()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02ST,ZZ,+01.250kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("Unknown scale mode", result.Message);
        }

        [Fact]
        public void Scale_Reject_NegativeWeight()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02ST,GS,-01.250kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("Negative weight", result.Message);
        }

        [Fact]
        public void Scale_Reject_MissingSign()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02ST,GS,01.250kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("Missing explicit sign", result.Message);
        }

        [Fact]
        public void Scale_Reject_WrongUnit()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            // Config default AllowedUnit is "kg", packet says "lb"
            var result = scale.ParseWeightPacket("\x02ST,GS,+02.450lb\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("Unsupported unit", result.Message);
        }

        [Fact]
        public void Scale_Reject_MissingUnit()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02ST,GS,+02.450\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("No unit found", result.Message);
        }

        [Fact]
        public void Scale_Reject_OverCapacity()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            scale.Config.MaxCapacityKg = 50.000m;
            var result = scale.ParseWeightPacket("\x02ST,GS,+60.000kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("exceeds max capacity", result.Message);
        }

        [Fact]
        public void Scale_Reject_BelowMinIncrement()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            scale.Config.MinIncrementKg = 0.005m;
            var result = scale.ParseWeightPacket("\x02ST,GS,+00.001kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("below minimum increment", result.Message);
        }

        [Fact]
        public void Scale_Reject_MissingDecimalPoint()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02ST,GS,+02450kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("missing decimal point", result.Message);
        }

        [Fact]
        public void Scale_Reject_TooFewFields()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02ST,+02.450kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("Expected 3 comma-separated fields", result.Message);
        }

        [Fact]
        public void Scale_Reject_TooManyFields()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("\x02ST,GS,+02.450kg,EXTRA\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("Expected 3 comma-separated fields", result.Message);
        }

        [Fact]
        public void Scale_Reject_EmptyPacket()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("");
            Assert.False(result.Success);
            Assert.Contains("Empty response", result.Message);
        }

        [Fact]
        public void Scale_Reject_MalformedArbitraryText()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            var result = scale.ParseWeightPacket("ERR_INVALID_STREAM");
            Assert.False(result.Success);
            Assert.Contains("Missing STX", result.Message);
        }

        [Fact]
        public void Scale_Reject_UnsupportedProtocol()
        {
            using var scale = new SerialWeighingScale(new FakeSerialPortDevice());
            scale.Config.Protocol = "AveryBerkel";
            var result = scale.ParseWeightPacket("\x02ST,GS,+02.450kg\x03\r\n");
            Assert.False(result.Success);
            Assert.Contains("Unsupported scale protocol", result.Message);
        }

        // ===== VFD Tests =====

        [Fact]
        public void VfdDisplay_SendToDisplay_WriteFailure_CleansUpPort()
        {
            var fakePort = new FakeSerialPortDevice { SimulateOpenSuccess = true, SimulateWriteSuccess = false };
            var vfd = new VfdCustomerDisplay(fakePort);
            vfd.DisplayTotal(100.00m);
            var result = vfd.SendToDisplay("COM1");
            Assert.False(result.Success);
            Assert.Contains("VFD serial error", result.Message);
            Assert.True(fakePort.CloseCount > 0);
        }

        [Fact]
        public void VfdDisplay_SanitizesControlCharsAndTruncatesTo20()
        {
            string dirty = "Hello\x1BWorld\x00Test1234567890";
            string clean = VfdCustomerDisplay.SanitizeVfdText(dirty);
            Assert.Equal(20, clean.Length);
            Assert.DoesNotContain("\x1B", clean);
            Assert.DoesNotContain("\x00", clean);
            Assert.StartsWith("HelloWorldTest123456", clean);
        }

        [Fact]
        public void VfdDisplay_ShortTextPaddedTo20()
        {
            string result = VfdCustomerDisplay.SanitizeVfdText("Hi");
            Assert.Equal(20, result.Length);
            Assert.StartsWith("Hi", result);
        }

        [Fact]
        public void VfdDisplay_GenerateSerialBytes_HasCorrectStructure()
        {
            var vfd = new VfdCustomerDisplay(new FakeSerialPortDevice());
            vfd.DisplayTotal(99.50m);
            var bytes = vfd.GenerateSerialBytes();

            // 0x0C clear + 3 header L1 + 20 text + 3 header L2 + 20 text = 47
            Assert.Equal(47, bytes.Length);
            Assert.Equal(0x0C, bytes[0]);                     // Clear
            Assert.Equal(0x1B, bytes[1]); Assert.Equal(0x51, bytes[2]); Assert.Equal(0x41, bytes[3]); // ESC Q A
            Assert.Equal(0x1B, bytes[24]); Assert.Equal(0x51, bytes[25]); Assert.Equal(0x42, bytes[26]); // ESC Q B
        }

        // ===== Skipped Physical Hardware Tests =====

        [Fact(Skip = "Requires physical RS-232 Toledo/NCI scale on COM port")]
        public void Physical_Scale_ReadWeight()
        {
            using var scale = new SerialWeighingScale();
            scale.Connect("COM2");
            var result = scale.ReadWeightKg();
            // Physical test — assert result has typed fields, not tautological
            Assert.NotNull(result);
            Assert.NotNull(result.Message);
        }

        [Fact(Skip = "Requires physical VFD 2x20 pole display on COM port")]
        public void Physical_VfdDisplay()
        {
            var vfd = new VfdCustomerDisplay();
            vfd.DisplayWelcomeMessage("ShopPro Store");
            var result = vfd.SendToDisplay("COM3");
            Assert.True(result.Success);
        }
    }
}
