using ShopPro.Core.Services;
using Xunit;

namespace ShopPro.Tests
{
    public class UpdateCheckerTests
    {
        [Fact]
        public async Task CheckForUpdates_ParsesManifestAndDetectsNewVersion()
        {
            // Arrange
            var service = new UpdateCheckerService();

            // Act
            var result = await service.CheckForUpdatesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.HasUpdate);
            Assert.Equal("1.1.0", result.Manifest.LatestVersion);
            Assert.NotEmpty(result.Manifest.ReleaseNotes);
        }
    }
}
