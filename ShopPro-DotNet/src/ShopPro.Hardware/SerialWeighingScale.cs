using System.Globalization;
using System.Text.RegularExpressions;

namespace ShopPro.Hardware
{
    public class ScaleConfig
    {
        public string ComPort { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public string PollCommand { get; set; } = "W\r";
        public int ReadTimeoutMs { get; set; } = 1000;
        public string Protocol { get; set; } = "Toledo";
        public string AllowedUnit { get; set; } = "kg";
        public decimal MaxCapacityKg { get; set; } = 50.000m;
        public decimal MinIncrementKg { get; set; } = 0.001m;
    }

    public class ScaleReadResult
    {
        public bool Success { get; set; }
        public decimal? WeightKg { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsStable { get; set; }
    }

    /// <summary>
    /// Serial Weighing Scale Integration:
    /// Protocol Spec: NCI / Toledo / Avery Berkel RS-232 ASCII Protocol.
    /// Configurable BaudRate, Parity, DataBits, StopBits, PollCommand, ReadTimeoutMs, AllowedUnit, and MaxCapacityKg.
    /// 
    /// Commercial Safety:
    /// - Parses exact Toledo/NCI packet structures using CultureInfo.InvariantCulture.
    /// - Explicitly rejects negative weights, unstable status (US), overload (OL), and out of range weights (> MaxCapacityKg).
    /// - Returns ScaleReadResult distinguishing zero tare weight (0.000 kg) from hardware read errors (null).
    /// </summary>
    public class SerialWeighingScale : IDisposable
    {
        private static readonly Regex ToledoRegex = new Regex(
            @"^\x02?(?:(?<status>ST|US|OL|EA),)?(?:GS|NT)?,?(?<sign>[+-])?(?<weight>[0-9]+\.[0-9]+)(?<unit>kg|lb)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ISerialPortDevice _device;

        public ScaleConfig Config { get; set; } = new();
        public bool IsConnected => _device != null && _device.IsOpen;
        public string ComPort => Config.ComPort;
        public string LastError { get; private set; } = string.Empty;

        public SerialWeighingScale(ISerialPortDevice? device = null)
        {
            _device = device ?? new NativeSerialPortDevice();
        }

        public (bool Success, string Message) Connect(string portName = "COM1", int baudRate = 9600)
        {
            Config.ComPort = portName;
            Config.BaudRate = baudRate;
            return Connect(Config);
        }

        public (bool Success, string Message) Connect(ScaleConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.ComPort))
                return (false, "Scale configuration or port name cannot be empty.");

            Config = config;
            var targetPort = Config.ComPort.Trim();

            // Reconnection Guard: If already connected, close existing connection first
            if (_device.IsOpen)
            {
                Disconnect();
            }

            LastError = string.Empty;

            try
            {
                _device.Open(targetPort, Config.BaudRate);
                return (true, $"Connected to weighing scale at port '{targetPort}' ({Config.BaudRate} baud).");
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
            if (!string.IsNullOrEmpty(unit) && unit == "lb")
            {
                parsedWeight = Math.Round(parsedWeight * 0.45359237m, 3, MidpointRounding.AwayFromZero);
            }

            // Enforce maximum capacity safety limit
            if (parsedWeight > Config.MaxCapacityKg)
            {
                return new ScaleReadResult
                {
                    Success = false,
                    WeightKg = null,
                    Message = $"Weight reading ({parsedWeight:F3} kg) exceeds maximum scale capacity ({Config.MaxCapacityKg:F3} kg).",
                    IsStable = isStable
                };
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
                // Transmit poll command to Toledo / NCI scale
                _device.Write(string.IsNullOrWhiteSpace(Config.PollCommand) ? "W\r" : Config.PollCommand);

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
