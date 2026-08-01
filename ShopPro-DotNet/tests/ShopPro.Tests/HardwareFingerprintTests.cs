using ShopPro.Core.Services;
using Xunit;

namespace ShopPro.Tests
{
    public class HardwareFingerprintTests
    {
        [Fact]
        public void GenerateMachineFingerprint_ReturnsFormattedUniqueHash()
        {
            // Act
            var hash1 = HardwareFingerprintGenerator.GenerateMachineFingerprint();
            var hash2 = HardwareFingerprintGenerator.GenerateMachineFingerprint();

            // Assert
            Assert.NotNull(hash1);
            Assert.StartsWith("HW-", hash1);
            Assert.Equal(hash1, hash2); // Deterministic on same machine
        }
    }
}
