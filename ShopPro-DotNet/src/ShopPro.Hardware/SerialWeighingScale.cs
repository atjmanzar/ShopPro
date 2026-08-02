using System.Globalization;
using System.Text.RegularExpressions;

namespace ShopPro.Hardware
{
    public class ScaleReadResult
    {
        public bool Success { get; set; }
        public decimal? WeightKg { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsStable { get; set; }
    }

    /// <summary>
    /// Serial Weighing Scale Integration:
    /// Protocol Spec: NCI / Toledo / Avery Berkel RS-232 ASCII Protocol (Baud: 9600, Data Bits: 8, Parity: None, Stop Bits: 1).
    /// Serial Command: 'W\r' (Poll weight command sent from POS to Scale).
    /// Response Format: ASCII string containing weight readings, e.g. "\x02ST,GS,+01.250kg\x03\r\n".
    /// 
    /// Commercial Safety:
    /// - Parses exact Toledo/NCI packet structures using CultureInfo.InvariantCulture.
    /// - Explicitly rejects negative weights, unstable status (US), overload (OL), and malformed packets.
    /// - Returns ScaleReadResult distinguishes zero tare weight (0.000 kg) from hardware read errors (null).
    /// </summary>
    public class SerialWeighingScale : IDisposable
    {
        private static readonly Regex ToledoRegex = new Regex(
            @"^\x02?(?:(?<status>ST|US|OL|EA),)?(?:GS|NT)?,?(?<sign>[+-])?(?<weight>[0-9]+\.[0-9]+)(?<unit>kg|lb)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ISerialPortDevice _device;

        public bool IsConnected => _device != null && _device.IsOpen;
        public string ComPort { get; private set; } = "COM1";
        public string LastError { get; private set; } = string.Empty;

        public SerialWeighingScale(ISerialPortDevice? device = null)
        {
            _device = device ?? new NativeSerialPortDevice();
        }

        public (bool Success, string Message) Connect(string portName = "COM1", int baudRate = 9600)
        {
            if (string.IsNullOrWhiteSpace(portName))
                return (false, "Port name cannot be empty.");

            var targetPort = portName.Trim();

            // Reconnection Guard: If already connected to a different port, close existing connection first
            if (_device.IsOpen)
            {
                Disconnect();
            }

            ComPort = targetPort;
            LastError = string.Empty;

            try
            {
                _device.Open(targetPort, baudRate);
                return (true, $"Connected to weighing scale at port '{targetPort}'.");
            }
            catch (Exception ex)
            {
                LastError = $"Failed to open serial port '{targetPort}': {ex.Message}";
                return (false, LastError);
            }
        }

        public ScaleReadResult ParseWeightPacket(string rawAsciiPacket)
        {
            if (string.IsNullOrWhiteSpace(rawAsciiPacket))
            {
                return new ScaleReadResult
                {
                    Success = false,
                    WeightKg = null,
                    Message = "Empty ASCII packet received from scale.",
                    IsStable = false
                };
            }

            var match = ToledoRegex.Match(rawAsciiPacket.Trim());
            if (!match.Success)
            {
                return new ScaleReadResult
                {
                    Success = false,
                    WeightKg = null,
                    Message = $"Malformed scale packet format: '{rawAsciiPacket.Trim()}'.",
                    IsStable = false
                };
            }

            string status = match.Groups["status"].Value.ToUpperInvariant();
            string sign = match.Groups["sign"].Value;
            string weightStr = match.Groups["weight"].Value;
            string unit = match.Groups["unit"].Value.ToLowerInvariant();

            // Reject Overload or Error status
            if (status == "OL")
            {
                return new ScaleReadResult { Success = false, WeightKg = null, Message = "Scale status: Overload (OL).", IsStable = false };
            }
            if (status == "EA")
            {
                return new ScaleReadResult { Success = false, WeightKg = null, Message = "Scale status: Error (EA).", IsStable = false };
            }

            // Reject Unstable status
            bool isStable = string.IsNullOrEmpty(status) || status == "ST";
            if (status == "US")
            {
                return new ScaleReadResult { Success = false, WeightKg = null, Message = "Scale reading is unstable (US). Wait for motion to stop.", IsStable = false };
            }

            // Explicitly reject negative signs (e.g. -01.250kg)
            if (sign == "-")
            {
                return new ScaleReadResult { Success = false, WeightKg = null, Message = "Negative weight reading rejected for checkout.", IsStable = isStable };
            }

            // Parse numeric weight using InvariantCulture
            if (!decimal.TryParse(weightStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedWeight))
            {
                return new ScaleReadResult { Success = false, WeightKg = null, Message = $"Invalid numeric value in packet: '{weightStr}'.", IsStable = isStable };
            }

            // Convert pounds to kg if unit is lb
            if (unit == "lb")
            {
                parsedWeight = Math.Round(parsedWeight * 0.45359237m, 3, MidpointRounding.AwayFromZero);
            }

            return new ScaleReadResult
            {
                Success = true,
                WeightKg = parsedWeight,
                Message = $"Valid {(isStable ? "stable" : "unstable")} scale reading: {parsedWeight:F3} kg.",
                IsStable = isStable
            };
        }

        public ScaleReadResult ReadWeightKg()
        {
            if (!IsConnected)
            {
                LastError = "Scale is not connected.";
                return new ScaleReadResult { Success = false, WeightKg = null, Message = LastError, IsStable = false };
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

                var result = ParseWeightPacket(rawPacket);
                if (result.Success)
                {
                    LastError = string.Empty; // Clear error on success
                }
                else
                {
                    LastError = result.Message; // Maintain error state
                }

                return result;
            }
            catch (TimeoutException)
            {
                LastError = "Serial scale read timed out.";
                return new ScaleReadResult { Success = false, WeightKg = null, Message = LastError, IsStable = false };
            }
            catch (Exception ex)
            {
                LastError = $"Scale read error: {ex.Message}";
                return new ScaleReadResult { Success = false, WeightKg = null, Message = LastError, IsStable = false };
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
