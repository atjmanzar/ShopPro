using System.Runtime.InteropServices;

namespace ShopPro.Hardware
{
    /// <summary>
    /// Win32 Spooler P/Invoke Helper for Direct RAW Printing:
    /// Transmits raw ESC/POS byte arrays directly to installed Windows printer drivers via OpenPrinterW / StartDocPrinterW in winspool.drv.
    /// Supports non-ASCII / Unicode Windows printer names.
    /// 
    /// Hardware Verification Note:
    /// Makes P/Invoke system calls to winspool.drv via IWin32SpoolerApi.
    /// Spooler acceptance means bytes were handed to the Windows Print Spooler. Physical paper print remains unverified without attached hardware.
    /// </summary>
    public static class WinSpoolPrinter
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class DOCINFOW
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pDocName = "ShopPro Receipt";
            [MarshalAs(UnmanagedType.LPWStr)] public string? pOutputFile = null;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDataType = "RAW";
        }

        public static (bool Success, string Message) SendBytesToPrinter(string printerName, byte[] bytes, IWin32SpoolerApi? api = null)
        {
            if (string.IsNullOrWhiteSpace(printerName))
                return (false, "Printer name is empty.");
            if (bytes == null || bytes.Length == 0)
                return (false, "Byte payload is empty.");

            var spoolerApi = api ?? new NativeWin32SpoolerApi();
            IntPtr hPrinter = IntPtr.Zero;
            IntPtr pUnmanagedBytes = IntPtr.Zero;
            bool docStarted = false;
            bool pageStarted = false;

            try
            {
                if (!spoolerApi.OpenPrinter(printerName.Trim(), out hPrinter, IntPtr.Zero) || hPrinter == IntPtr.Zero)
                {
                    int err = spoolerApi.GetLastError();
                    return (false, $"Printer '{printerName}' not found or inaccessible (Win32 Error: {err}). Check USB/network connection.");
                }

                var di = new DOCINFOW();
                uint jobId = spoolerApi.StartDocPrinter(hPrinter, 1, di);
                if (jobId == 0)
                {
                    int err = spoolerApi.GetLastError();
                    return (false, $"StartDocPrinter failed for '{printerName}' (Win32 Error: {err}).");
                }
                docStarted = true;

                if (!spoolerApi.StartPagePrinter(hPrinter))
                {
                    int err = spoolerApi.GetLastError();
                    return (false, $"StartPagePrinter failed for '{printerName}' (Win32 Error: {err}).");
                }
                pageStarted = true;

                pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);

                bool writeOk = spoolerApi.WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out int dwWritten);

                if (!writeOk)
                {
                    int err = spoolerApi.GetLastError();
                    return (false, $"WritePrinter failed for '{printerName}' (Win32 Error: {err}).");
                }

                if (dwWritten != bytes.Length)
                {
                    return (false, $"Partial write to spooler for '{printerName}': wrote {dwWritten} of {bytes.Length} bytes.");
                }

                return (true, $"Byte stream successfully handed to Windows Print Spooler (Job ID: {jobId}) for target '{printerName}'. Physical paper print unverified without attached hardware.");
            }
            catch (Exception ex)
            {
                return (false, $"Spooler exception for '{printerName}': {ex.Message}");
            }
            finally
            {
                if (pUnmanagedBytes != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pUnmanagedBytes);
                }
                if (pageStarted && hPrinter != IntPtr.Zero)
                {
                    spoolerApi.EndPagePrinter(hPrinter);
                }
                if (docStarted && hPrinter != IntPtr.Zero)
                {
                    spoolerApi.EndDocPrinter(hPrinter);
                }
                if (hPrinter != IntPtr.Zero)
                {
                    spoolerApi.ClosePrinter(hPrinter);
                }
            }
        }
    }
}
