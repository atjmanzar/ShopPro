using System.Text.RegularExpressions;

namespace ShopPro.Hardware
{
    /// <summary>
    /// Serial Weighing Scale Integration:
    /// Protocol Spec: NCI / Toledo / Avery Berkel RS-232 ASCII Protocol (Baud: 9600, Data Bits: 8, Parity: None, Stop Bits: 1).
    /// Serial Command: 'W\r' (Poll weight command sent from POS to Scale).
    /// Response Format: ASCII string containing weight readings, e.g. "\x02ST,GS,+01.250kg\x03\r\n".
    /// 
    /// Note on Verification:
    /// Uses injectable ISerialPortDevice. Connect attempts to open real serial port. ReadWeightKg transmits 'W\r' poll command over serial interface, reads ASCII response stream, and parses weight via Regex.
    /// Hardware Verification: Physical scale reading requires attached RS-232 Toledo/NCI weighing scale.
    /// </summary>
    public class SerialWeighingScale : IDisposable
    {
        private ISerialPortDevice _device;

        public bool IsConnected => _device != null && _device.IsOpen;
        public string ComPort { get; private set; } = "COM1";
        public string LastError { get; private set; } = string.Empty;

        public SerialWeighingScale(ISerialPortDevice? device = null)
        {
            _device = device ?? new NativeSerialPortDevice();
        }

        public (bool Success, string Message) Connect(string portName = "COM1", int baudRate = 9600)
        {
            ComPort = portName;
            LastError = string.Empty;

            try
            {
                if (!_device.IsOpen)
                {
                    _device.Open(portName, baudRate);
                }
                return (true, $"Connected to weighing scale at port '{portName}'.");
            }
            catch (Exception ex)
            {
                LastError = $"Failed to open serial port '{portName}': {ex.Message}";
                return (false, LastError);
            }
        }

        public decimal ParseWeightPacket(string rawAsciiPacket)
        {
            if (string.IsNullOrWhiteSpace(rawAsciiPacket)) return 0.000m;

            // Regex matches numbers with decimals in weight string (e.g. "+01.250kg" -> 1.250)
            var match = Regex.Match(rawAsciiPacket, @"([0-9]+\.[0-9]+)");
            if (match.Success && decimal.TryParse(match.Value, out var weight))
            {
                return weight;
            }

            return 0.000m;
        }

        public decimal ReadWeightKg()
        {
            if (!IsConnected)
            {
                LastError = "Scale is not connected.";
                return 0.000m;
            }

            try
            {
                // Transmit 'W\r' ASCII poll command to Toledo / NCI scale
                _device.Write("W\r");

                // Read ASCII response from serial buffer
                string rawPacket = _device.ReadLine();
                if (string.IsNullOrWhiteSpace(rawPacket))
                {
                    rawPacket = _device.ReadExisting();
                }

                if (string.IsNullOrWhiteSpace(rawPacket))
                {
                    LastError = "Empty response from scale.";
                    return 0.000m;
                }

                return ParseWeightPacket(rawPacket);
            }
            catch (TimeoutException)
            {
                LastError = "Serial scale read timed out.";
                return 0.000m;
            }
            catch (Exception ex)
            {
                LastError = $"Scale read error: {ex.Message}";
                return 0.000m;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_device.IsOpen)
                {
                    _device.Close();
                }
            }
            catch
            {
                // Clean close
            }
        }

        public void Dispose()
        {
            Disconnect();
            _device?.Dispose();
        }
    }
}
