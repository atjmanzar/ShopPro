using System.Security.Cryptography;
using System.Text;

namespace ShopPro.Core.Services
{
    public class HardwareFingerprintGenerator
    {
        public static string GenerateMachineFingerprint()
        {
            var rawInfo = $"{Environment.MachineName}-{Environment.UserName}-{Environment.ProcessorCount}-{Environment.OSVersion}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawInfo));

            var hex = Convert.ToHexString(bytes);
            return $"HW-{hex.Substring(0, 4)}-{hex.Substring(4, 4)}-{hex.Substring(8, 4)}-{hex.Substring(12, 4)}";
        }
    }
}
