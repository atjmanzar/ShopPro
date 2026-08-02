using System.Text;
using System.Text.RegularExpressions;

namespace ShopPro.Hardware
{
    /// <summary>
    /// Receipt Text & Control Byte Sanitizer:
    /// Strips ESC/POS control characters (ESC 0x1B, GS 0x1D, NUL 0x00, control bytes 0x00-0x1F) and line breaks from untrusted input text
    /// before constructing raw binary ESC/POS byte streams.
    /// Enforces bounded field lengths and deterministic line wrapping/truncation.
    /// </summary>
    public static class ReceiptSanitizer
    {
        private static readonly Regex ControlCharRegex = new Regex(@"[\x00-\x1F\x7F]", RegexOptions.Compiled);

        public static string SanitizeLineText(string untrustedText, int maxLength = 48)
        {
            if (string.IsNullOrEmpty(untrustedText)) return string.Empty;

            // Replace line breaks (\r, \n) with a single space
            string noLineBreaks = untrustedText.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

            // Strip all ESC/POS control characters (0x00 - 0x1F and 0x7F)
            string cleaned = ControlCharRegex.Replace(noLineBreaks, string.Empty).Trim();

            // Truncate to maximum allowable field width
            if (cleaned.Length > maxLength)
            {
                cleaned = cleaned.Substring(0, maxLength);
            }

            return cleaned;
        }

        public static string SanitizeFilename(string untrustedFilename)
        {
            if (string.IsNullOrEmpty(untrustedFilename)) return "Receipt";

            string safe = string.Concat(untrustedFilename.Split(Path.GetInvalidFileNameChars()));
            safe = ControlCharRegex.Replace(safe, string.Empty).Trim();

            return string.IsNullOrWhiteSpace(safe) ? "Receipt" : safe;
        }
    }
}
