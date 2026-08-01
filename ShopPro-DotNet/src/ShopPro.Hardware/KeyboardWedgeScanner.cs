using System.Text;

namespace ShopPro.Hardware
{
    public class BarcodeScannedEventArgs : EventArgs
    {
        public string Barcode { get; }

        public BarcodeScannedEventArgs(string barcode)
        {
            Barcode = barcode;
        }
    }

    /// <summary>
    /// Hardware Keyboard-Wedge USB Barcode Scanner Buffer.
    /// Distinguishes human typing from high-speed USB scanner keystrokes (< 50ms per key) ending with Enter.
    /// </summary>
    public class KeyboardWedgeScanner
    {
        private readonly StringBuilder _buffer = new();
        private DateTime _lastKeystrokeTime = DateTime.MinValue;
        private readonly TimeSpan _maxInterKeyDelay = TimeSpan.FromMilliseconds(60);

        public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

        public void ProcessKeystroke(char keyChar)
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastKeystrokeTime;
            _lastKeystrokeTime = now;

            if (keyChar == '\r' || keyChar == '\n')
            {
                if (_buffer.Length >= 3)
                {
                    var barcode = _buffer.ToString().Trim();
                    _buffer.Clear();
                    BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(barcode));
                }
                else
                {
                    _buffer.Clear();
                }
                return;
            }

            if (elapsed > _maxInterKeyDelay)
            {
                // Delay too long; reset buffer to avoid mixing human typing
                _buffer.Clear();
            }

            if (!char.IsControl(keyChar))
            {
                _buffer.Append(keyChar);
            }
        }
    }
}
