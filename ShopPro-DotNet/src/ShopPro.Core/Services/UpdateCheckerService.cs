using System.Text.Json;

namespace ShopPro.Core.Services
{
    public class UpdateManifestInfo
    {
        public string LatestVersion { get; set; } = "1.0.0";
        public string ReleaseDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
        public bool IsMandatory { get; set; } = false;
        public string DownloadUrl { get; set; } = string.Empty;
        public List<string> ReleaseNotes { get; set; } = new();
    }

    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public string CurrentVersion { get; set; } = "1.0.0";
        public UpdateManifestInfo Manifest { get; set; } = new();
    }

    public class UpdateCheckerService
    {
        public const string CurrentAppVersion = "1.0.0";

        public async Task<UpdateCheckResult> CheckForUpdatesAsync(string manifestJsonOrUrl = "")
        {
            UpdateManifestInfo manifest;

            if (string.IsNullOrWhiteSpace(manifestJsonOrUrl) || !manifestJsonOrUrl.Trim().StartsWith("{"))
            {
                // Simulated remote manifest parser response
                manifest = new UpdateManifestInfo
                {
                    LatestVersion = "1.1.0",
                    ReleaseDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    IsMandatory = false,
                    DownloadUrl = "https://github.com/atjmanzar/ShopPro/releases/download/v1.1.0/ShopPro_Setup_v1.1.0.exe",
                    ReleaseNotes = new List<string>
                    {
                        "• Added Auto-Update Checker and Remote Manifest Parser.",
                        "• Performance optimizations for 100k product catalogs.",
                        "• Enhanced thermal receipt printing engine."
                    }
                };
            }
            else
            {
                manifest = JsonSerializer.Deserialize<UpdateManifestInfo>(manifestJsonOrUrl, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new UpdateManifestInfo();
            }

            bool hasUpdate = SemanticVersionComparer.IsUpdateAvailable(CurrentAppVersion, manifest.LatestVersion);

            await Task.CompletedTask;
            return new UpdateCheckResult
            {
                HasUpdate = hasUpdate,
                CurrentVersion = CurrentAppVersion,
                Manifest = manifest
            };
        }

        public async Task<string> DownloadUpdatePackageAsync(string downloadUrl, Action<int>? progressCallback = null)
        {
            var tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShopPro", "Updates");
            if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

            var installerPath = Path.Combine(tempFolder, "ShopPro_Setup_Update.exe");

            // Simulate package download progress
            for (int i = 10; i <= 100; i += 30)
            {
                progressCallback?.Invoke(i);
                await Task.Delay(50);
            }

            File.WriteAllText(installerPath, "ShopPro Installer Stub Executable");
            return installerPath;
        }
    }
}
