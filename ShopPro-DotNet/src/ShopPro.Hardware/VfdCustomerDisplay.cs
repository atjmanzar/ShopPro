namespace ShopPro.Hardware
{
    /// <summary>
    /// VFD Customer Display Integration:
    /// Protocol Spec: Standard ESC/POS VFD Command Set (Epson / Futaba / Posiflex 2x20 Character Display).
    /// Serial Protocol Commands:
    /// - Clear Display: 0x0C (Form Feed)
    /// - Position Line 1: 0x1B, 0x51, 0x41 (ESC Q A)
    /// - Position Line 2: 0x1B, 0x51, 0x42 (ESC Q B)
    /// 
    /// Note on Testability & Transport:
    /// Generates binary ESC/POS VFD control streams and transmits them over serial port via injectable ISerialPortDevice.
    /// Hardware Verification: Physical display requires attached VFD 2x20 pole display.
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
            Line1Text = "WELCOME TO";
            Line2Text = storeName.Length > 20 ? storeName.Substring(0, 20) : storeName;
        }

        public void DisplayItemScanned(string itemName, decimal price)
        {
            Line1Text = itemName.Length > 20 ? itemName.Substring(0, 20) : itemName;
            Line2Text = $"Price: Rs. {price:F2}";
        }

        public void DisplayTotal(decimal grandTotal)
        {
            Line1Text = "TOTAL DUE:";
            Line2Text = $"Rs. {grandTotal:F2}";
        }

        public byte[] GenerateSerialBytes()
        {
            var bytes = new List<byte>();
            // 0x0C = Clear Screen
            bytes.Add(0x0C);

            // Line 1: ESC Q A + Line1Text padded to 20 chars
            bytes.AddRange(new byte[] { 0x1B, 0x51, 0x41 });
            var line1Padded = Line1Text.PadRight(20).Substring(0, 20);
            bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(line1Padded));

            // Line 2: ESC Q B + Line2Text padded to 20 chars
            bytes.AddRange(new byte[] { 0x1B, 0x51, 0x42 });
            var line2Padded = Line2Text.PadRight(20).Substring(0, 20);
            bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(line2Padded));

            return bytes.ToArray();
        }

        public (bool Success, string Message) SendToDisplay(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
                return (false, "Port name is empty.");

            try
            {
                byte[] bytes = GenerateSerialBytes();
                _device.Open(portName);
                _device.Write(bytes, 0, bytes.Length);
                _device.Close();
                return (true, $"VFD bytes transmitted to port '{portName}'. Physical text display unverified without attached hardware.");
            }
            catch (Exception ex)
            {
                return (false, $"VFD serial error ({portName}): {ex.Message}");
            }
        }
    }
}
