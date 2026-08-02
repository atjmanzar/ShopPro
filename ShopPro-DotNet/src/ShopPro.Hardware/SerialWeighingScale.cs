using System.Globalization;
using System.IO.Ports;

namespace ShopPro.Hardware
{
    /// <summary>
    /// Scale serial configuration.
    /// Every declared setting is used:
    /// port, baud, parity, data bits, stop bits, read timeout, write timeout,
    /// poll command, protocol name, allowed unit, max capacity, and min increment.
    /// </summary>
    public class ScaleConfig
    {
        public string ComPort { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public int ReadTimeoutMs { get; set; } = 1000;
        public int WriteTimeoutMs { get; set; } = 1000;
        public string PollCommand { get; set; } = "W\r";
        public string Protocol { get; set; } = "Toledo";
        public string AllowedUnit { get; set; } = "kg";
        public decimal MaxCapacityKg { get; set; } = 50.000m;
        public decimal MinIncrementKg { get; set; } = 0.001m;
    }

    /// <summary>
    /// Typed weight reading result.
    /// Zero weight (0.000 kg) is Success=true with WeightKg=0.
    /// Read error is Success=false with WeightKg=null.
    /// </summary>
    public class ScaleReadResult
    {
        public bool Success { get; set; }
        public decimal? WeightKg { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsStable { get; set; }
    }

    /// <summary>
    /// Serial Weighing Scale Integration.
    ///
    /// Supported protocol: "Toledo" — exact NCI/Toledo 8142/8213 ASCII continuous output.
    /// Packet format: STX status,mode,sign digits.digits unit ETX CR LF
    ///   STX  = 0x02
    ///   status = ST (stable) | US (unstable) | OL (overload) | EA (error)
    ///   mode = GS (gross) | NT (net)
    ///   sign = + | -
    ///   digits = exactly NN.NNN (5+ chars with decimal)
    ///   unit = kg | lb
    ///   ETX = 0x03
    ///   CR LF = \r\n
    ///
    /// Rejected inputs:
    ///   bare decimals, missing STX/ETX, trailing junk, unknown status,
    ///   wrong unit vs AllowedUnit, negative sign, unstable, overload, error,
    ///   weight below MinIncrementKg or above MaxCapacityKg, malformed precision.
    /// </summary>
    public class SerialWeighingScale : IDisposable
    {
        private readonly ISerialPortDevice _device;

        public ScaleConfig Config { get; private set; } = new();
        public bool IsConnected => _device != null && _device.IsOpen;
        public string ComPort => Config.ComPort;
        public string LastError { get; private set; } = string.Empty;

        public SerialWeighingScale(ISerialPortDevice? device = null)
        {
            _device = device ?? new NativeSerialPortDevice();
        }

        public (bool Success, string Message) Connect(string portName, int baudRate = 9600)
        {
            var cfg = new ScaleConfig { ComPort = portName, BaudRate = baudRate };
            return Connect(cfg);
        }

        public (bool Success, string Message) Connect(ScaleConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.ComPort))
                return (false, "Scale configuration or port name cannot be empty.");

            Config = config;
            var targetPort = Config.ComPort.Trim();

            // Reconnection guard: close existing before opening different port
            if (_device.IsOpen)
            {
                Disconnect();
            }

            LastError = string.Empty;

            try
            {
                _device.Open(new SerialPortConfig
                {
                    PortName = targetPort,
                    BaudRate = Config.BaudRate,
                    Parity = Config.Parity,
                    DataBits = Config.DataBits,
                    StopBits = Config.StopBits,
                    ReadTimeoutMs = Config.ReadTimeoutMs,
                    WriteTimeoutMs = Config.WriteTimeoutMs
                });
                return (true, $"Connected to weighing scale at port '{targetPort}' ({Config.BaudRate} baud, {Config.Parity}, {Config.DataBits}, {Config.StopBits}).");
            }
            catch (Exception ex)
            {
                LastError = $"Failed to open serial port '{targetPort}': {ex.Message}";
                return (false, LastError);
            }
        }

        /// <summary>
        /// Parse a raw ASCII packet using the protocol selected in Config.Protocol.
        /// Only "Toledo" is currently implemented. Unknown protocols return failure.
        /// </summary>
        public ScaleReadResult ParseWeightPacket(string rawPacket)
        {
            if (string.IsNullOrEmpty(rawPacket))
            {
                return Fail("Empty response received from scale.");
            }

            return Config.Protocol switch
            {
                "Toledo" => ParseToledoPacket(rawPacket),
                _ => Fail($"Unsupported scale protocol: '{Config.Protocol}'. Only 'Toledo' is implemented.")
            };
        }

        /// <summary>
        /// Exact Toledo/NCI packet parser.
        /// Required packet: STX status,mode,sign digits.digits unit ETX CR LF
        /// Every field is validated; bare decimals, missing framing, trailing junk are rejected.
        /// </summary>
        private ScaleReadResult ParseToledoPacket(string raw)
        {
            // Strip trailing CR/LF for parsing but require they were present
            var trimmed = raw.TrimEnd('\r', '\n');

            // 1. Require STX (0x02) at start
            if (trimmed.Length == 0 || trimmed[0] != '\x02')
            {
                return Fail($"Missing STX (0x02) at start of packet: '{Escape(raw)}'.");
            }

            // 2. Require ETX (0x03) at end
            if (trimmed[trimmed.Length - 1] != '\x03')
            {
                return Fail($"Missing ETX (0x03) at end of packet: '{Escape(raw)}'.");
            }

            // 3. Extract body between STX and ETX
            string body = trimmed.Substring(1, trimmed.Length - 2);

            // 4. Split by comma — expect exactly 3 fields: status, mode, weightWithUnit
            string[] fields = body.Split(',');
            if (fields.Length != 3)
            {
                return Fail($"Expected 3 comma-separated fields (status,mode,weight+unit), got {fields.Length}: '{Escape(raw)}'.");
            }

            string statusField = fields[0].Trim().ToUpperInvariant();
            string modeField = fields[1].Trim().ToUpperInvariant();
            string weightUnitField = fields[2].Trim();

            // 5. Validate status
            switch (statusField)
            {
                case "OL":
                    return Fail("Scale overload (OL).");
                case "EA":
                    return Fail("Scale error (EA).");
                case "US":
                    return Fail("Scale reading unstable (US). Wait for motion to stop.");
                case "ST":
                    break; // stable — continue
                default:
                    return Fail($"Unknown scale status: '{statusField}'. Expected ST, US, OL, or EA.");
            }

            // 6. Validate mode
            if (modeField != "GS" && modeField != "NT")
            {
                return Fail($"Unknown scale mode: '{modeField}'. Expected GS (gross) or NT (net).");
            }

            // 7. Parse sign — must be explicit + or -
            if (weightUnitField.Length < 2)
            {
                return Fail($"Weight+unit field too short: '{weightUnitField}'.");
            }

            char signChar = weightUnitField[0];
            if (signChar != '+' && signChar != '-')
            {
                return Fail($"Missing explicit sign (+/-) in weight field: '{weightUnitField}'.");
            }
            if (signChar == '-')
            {
                return Fail("Negative weight reading rejected for checkout.");
            }

            // 8. Split numeric part from unit
            string afterSign = weightUnitField.Substring(1);

            // Find where unit starts (first alpha char)
            int unitStart = -1;
            for (int i = 0; i < afterSign.Length; i++)
            {
                if (char.IsLetter(afterSign[i]))
                {
                    unitStart = i;
                    break;
                }
            }

            if (unitStart < 0)
            {
                return Fail($"No unit found in weight field: '{weightUnitField}'.");
            }

            string numericStr = afterSign.Substring(0, unitStart);
            string unitStr = afterSign.Substring(unitStart).ToLowerInvariant();

            // 9. Validate unit against AllowedUnit
            if (unitStr != Config.AllowedUnit.ToLowerInvariant())
            {
                return Fail($"Unsupported unit '{unitStr}'. Expected '{Config.AllowedUnit}'.");
            }

            // 10. Parse numeric with InvariantCulture — must contain a decimal point
            if (!numericStr.Contains('.'))
            {
                return Fail($"Weight value missing decimal point: '{numericStr}'.");
            }

            if (!decimal.TryParse(numericStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal weight))
            {
                return Fail($"Invalid numeric weight value: '{numericStr}'.");
            }

            // 11. Enforce max capacity
            if (weight > Config.MaxCapacityKg)
            {
                return Fail($"Weight {weight:F3} {Config.AllowedUnit} exceeds max capacity {Config.MaxCapacityKg:F3} {Config.AllowedUnit}.");
            }

            // 12. Enforce min increment (non-zero readings must meet minimum)
            if (weight > 0m && weight < Config.MinIncrementKg)
            {
                return Fail($"Weight {weight:F3} {Config.AllowedUnit} below minimum increment {Config.MinIncrementKg:F3} {Config.AllowedUnit}.");
            }

            return new ScaleReadResult
            {
                Success = true,
                WeightKg = weight,
                IsStable = true,
                Message = $"Stable reading: {weight:F3} {Config.AllowedUnit}."
            };
        }

        public ScaleReadResult ReadWeightKg()
        {
            if (!IsConnected)
            {
                LastError = "Scale is not connected.";
                return Fail(LastError);
            }

            try
            {
                _device.Write(string.IsNullOrWhiteSpace(Config.PollCommand) ? "W\r" : Config.PollCommand);

                string rawPacket = _device.ReadLine();
                if (string.IsNullOrWhiteSpace(rawPacket))
                {
                    rawPacket = _device.ReadExisting();
                }

                var result = ParseWeightPacket(rawPacket);
                if (result.Success)
                {
                    LastError = string.Empty;
                }
                else
                {
                    LastError = result.Message;
                }
                return result;
            }
            catch (TimeoutException)
            {
                LastError = "Serial scale read timed out.";
                return Fail(LastError);
            }
            catch (Exception ex)
            {
                LastError = $"Scale read error: {ex.Message}";
                return Fail(LastError);
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_device.IsOpen) _device.Close();
            }
            catch { }
        }

        public void Dispose()
        {
            Disconnect();
            _device?.Dispose();
        }

        private static ScaleReadResult Fail(string message)
        {
            return new ScaleReadResult { Success = false, WeightKg = null, Message = message, IsStable = false };
        }

        private static string Escape(string s)
        {
            return s.Replace("\x02", "<STX>").Replace("\x03", "<ETX>").Replace("\r", "<CR>").Replace("\n", "<LF>");
        }
    }
}
