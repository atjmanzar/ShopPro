using ShopPro.Core.Services;
using Xunit;

namespace ShopPro.Tests
{
    public class VersionComparerTests
    {
        [Theory]
        [InlineData("1.0.0", "1.1.0", true)]
        [InlineData("1.0.0", "2.0.0", true)]
        [InlineData("1.1.0", "1.0.0", false)]
        [InlineData("1.0.0", "1.0.0", false)]
        [InlineData("v1.0.0", "v1.0.1", true)]
        public void IsUpdateAvailable_EvaluatesSemanticVersionsCorrectly(string current, string latest, bool expected)
        {
            // Act
            var result = SemanticVersionComparer.IsUpdateAvailable(current, latest);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
