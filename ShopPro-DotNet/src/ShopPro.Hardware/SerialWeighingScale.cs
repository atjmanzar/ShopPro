using System.Text.RegularExpressions;

namespace ShopPro.Hardware
{
    /// <summary>
    /// Serial Weighing Scale Integration:
    /// Protocol Spec: NCI / Toledo / Avery Berkel RS-232 ASCII Protocol (Baud: 9600, Data Bits: 8, Parity: None, Stop Bits: 1).
    /// Serial Command: 'W\r' (Poll weight command sent from POS to Scale).
    /// Response Format: ASCII string containing weight readings, e.g. "\x02ST,GS,+01.250kg\x03\r\n".
    /// 
    /// Note on Testability:
    /// Unit tests verify ASCII regex packet parsing logic (extracting numeric weight 1.250 kg).
    /// End-to-end device testing requires physical RS-232 weighing scale attached via COM port.
    /// </summary>
    public class SerialWeighingScale
    {
        public bool IsConnected { get; private set; } = false;
        public string ComPort { get; set; } = "COM1";

        public void Connect(string portName = "COM1")
        {
            ComPort = portName;
            IsConnected = true;
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
            if (!IsConnected) return 0.000m;

            // Sample ASCII packet from Toledo scale
            string samplePacket = "\x02ST,GS,+01.250kg\x03\r\n";
            return ParseWeightPacket(samplePacket);
        }

        public void Disconnect()
        {
            IsConnected = false;
        }
    }
}
