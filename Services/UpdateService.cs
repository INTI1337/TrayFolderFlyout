using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TrayFolderFlyout.Services
{
    public class UpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public string LatestVersion { get; set; } = "";
        public string ReleaseUrl { get; set; } = "https://github.com/INTI1337/TrayFolderFlyout/releases/latest";
        public string ReleaseNotes { get; set; } = "";
    }

    public static class UpdateService
    {
        public const string CurrentVersion = "v1.2.0";
        private const string ApiUrl = "https://api.github.com/repos/INTI1337/TrayFolderFlyout/releases/latest";

        public static async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            var result = new UpdateInfo();
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(6);
                client.DefaultRequestHeaders.Add("User-Agent", "TrayFolderFlyout-App");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

                string json = await client.GetStringAsync(ApiUrl);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("tag_name", out var tagElem))
                {
                    string latestTag = tagElem.GetString() ?? "";
                    result.LatestVersion = latestTag;

                    if (root.TryGetProperty("html_url", out var urlElem))
                    {
                        result.ReleaseUrl = urlElem.GetString() ?? result.ReleaseUrl;
                    }

                    if (root.TryGetProperty("body", out var bodyElem))
                    {
                        result.ReleaseNotes = bodyElem.GetString() ?? "";
                    }

                    result.IsUpdateAvailable = IsNewer(latestTag, CurrentVersion);
                }
            }
            catch (Exception ex)
            {
                App.Log($"[UpdateService] Error checking for updates: {ex.Message}");
            }

            return result;
        }

        public static bool IsNewer(string latestTag, string currentTag)
        {
            try
            {
                string v1 = latestTag.TrimStart('v', 'V').Trim();
                string v2 = currentTag.TrimStart('v', 'V').Trim();

                if (Version.TryParse(v1, out var parsedLatest) && Version.TryParse(v2, out var parsedCurrent))
                {
                    return parsedLatest > parsedCurrent;
                }

                return !string.Equals(latestTag, currentTag, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static void OpenReleasePage(string? url = null)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = string.IsNullOrEmpty(url) ? "https://github.com/INTI1337/TrayFolderFlyout/releases/latest" : url,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
