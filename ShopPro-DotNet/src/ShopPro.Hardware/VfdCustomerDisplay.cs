namespace ShopPro.Hardware
{
    /// <summary>
    /// VFD Customer Pole Display Integration.
    /// Protocol: Standard ESC/POS VFD command set (Epson / Futaba / Posiflex 2x20 character display).
    ///   Clear Display: 0x0C (Form Feed)
    ///   Position Line 1: 0x1B, 0x51, 0x41 (ESC Q A)
    ///   Position Line 2: 0x1B, 0x51, 0x42 (ESC Q B)
    ///
    /// Text is sanitised to printable ASCII (0x20-0x7E) and restricted to 20 characters per line.
    /// Serial resources are always closed/disposed in finally, including partial-open failures.
    /// Uses the same injectable ISerialPortDevice as printer and scale paths.
    ///
    /// VFD success means bytes were written to an opened serial device;
    /// it does not mean the customer saw the text.
    /// </summary>
    public class VfdCustomerDisplay
    {
        private readonly ISerialPortDevice _device;

        public string Line1Text { get; private set; } = string.Empty;
        public string Line2Text { get; private set; } = string.Empty;

        public VfdCustomerDisplay(ISerialPortDevice? device = null)
        {
            _device = device ?? new NativeSerialPortDevice();
        }

        public void ClearDisplay()
        {
            Line1Text = string.Empty;
            Line2Text = string.Empty;
        }

        public void DisplayWelcomeMessage(string storeName)
        {
            Line1Text = SanitizeVfdText("WELCOME TO");
            Line2Text = SanitizeVfdText(storeName);
        }

        public void DisplayItemScanned(string itemName, decimal price)
        {
            Line1Text = SanitizeVfdText(itemName);
            Line2Text = SanitizeVfdText($"Price: Rs. {price:F2}");
        }

        public void DisplayTotal(decimal grandTotal)
        {
            Line1Text = SanitizeVfdText("TOTAL DUE:");
            Line2Text = SanitizeVfdText($"Rs. {grandTotal:F2}");
        }

        /// <summary>
        /// Sanitise text for VFD: strip control characters, restrict to printable ASCII (0x20-0x7E),
        /// and truncate/pad to exactly 20 characters.
        /// </summary>
        public static string SanitizeVfdText(string text)
        {
            if (string.IsNullOrEmpty(text)) return new string(' ', 20);

            var sb = new System.Text.StringBuilder(20);
            foreach (char c in text)
            {
                if (c >= 0x20 && c <= 0x7E)
                {
                    sb.Append(c);
                    if (sb.Length >= 20) break;
                }
            }
            return sb.ToString().PadRight(20).Substring(0, 20);
        }

        public byte[] GenerateSerialBytes()
        {
            var bytes = new List<byte>();
            // 0x0C = Clear Screen
            bytes.Add(0x0C);

            // Line 1: ESC Q A + 20 chars
            bytes.AddRange(new byte[] { 0x1B, 0x51, 0x41 });
            bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(SanitizeVfdText(Line1Text)));

            // Line 2: ESC Q B + 20 chars
            bytes.AddRange(new byte[] { 0x1B, 0x51, 0x42 });
            bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(SanitizeVfdText(Line2Text)));

            return bytes.ToArray();
        }

        public (bool Success, string Message) SendToDisplay(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
                return (false, "VFD port name is empty.");

            bool opened = false;
            try
            {
                byte[] bytes = GenerateSerialBytes();
                _device.Open(portName);
                opened = true;

                _device.Write(bytes, 0, bytes.Length);
                return (true, $"VFD bytes transmitted to port '{portName}'. Customer text display unverified without attached hardware.");
            }
            catch (Exception ex)
            {
                return (false, $"VFD serial error ({portName}): {ex.Message}");
            }
            finally
            {
                if (opened || _device.IsOpen)
                {
                    try { _device.Close(); } catch { }
                }
            }
        }
    }
}
