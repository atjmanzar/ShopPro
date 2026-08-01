namespace ShopPro.Core.Services
{
    public class SemanticVersionComparer
    {
        public static bool IsUpdateAvailable(string currentVersion, string latestVersion)
        {
            if (string.IsNullOrWhiteSpace(currentVersion) || string.IsNullOrWhiteSpace(latestVersion)) return false;

            var curr = ParseVersion(currentVersion);
            var late = ParseVersion(latestVersion);

            return late > curr;
        }

        public static Version ParseVersion(string versionStr)
        {
            var cleaned = versionStr.TrimStart('v', 'V').Split('-')[0];
            if (Version.TryParse(cleaned, out var v))
            {
                return v;
            }
            return new Version(1, 0, 0);
        }
    }
}
