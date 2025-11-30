using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Mass.Core.Updates;

public class UpdateService : IUpdateService
{
    private const string GitHubApiUrl = "https://api.github.com/repos/YourOrg/ProUSBMediaSuite/releases/latest";
    private readonly HttpClient _httpClient;

    public string CurrentVersion { get; }
    public event EventHandler<UpdateStatus>? StatusChanged;

    public UpdateService()
    {
        CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ProUSBMediaSuite");
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        StatusChanged?.Invoke(this, UpdateStatus.CheckingForUpdates);

        try
        {
            var response = await _httpClient.GetStringAsync(GitHubApiUrl);
            var release = JsonSerializer.Deserialize<GitHubRelease>(response);

            if (release == null)
            {
                return new UpdateCheckResult
                {
                    UpdateAvailable = false,
                    CurrentVersion = CurrentVersion,
                    Message = "Unable to check for updates"
                };
            }

            var latestVersion = release.TagName.TrimStart('v');
            var isNewer = CompareVersions(latestVersion, CurrentVersion) > 0;

            if (isNewer)
            {
                var asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip"));
                if (asset != null)
                {
                    StatusChanged?.Invoke(this, UpdateStatus.UpdateAvailable);
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = true,
                        CurrentVersion = CurrentVersion,
                        LatestVersion = new UpdateInfo
                        {
                            Version = latestVersion,
                            ReleaseNotes = release.Body,
                            DownloadUrl = asset.BrowserDownloadUrl,
                            PublishedAt = release.PublishedAt,
                            SizeBytes = asset.Size,
                            IsPrerelease = release.Prerelease
                        },
                        Message = $"Version {latestVersion} is available"
                    };
                }
            }

            StatusChanged?.Invoke(this, UpdateStatus.UpToDate);
            return new UpdateCheckResult
            {
                UpdateAvailable = false,
                CurrentVersion = CurrentVersion,
                Message = "You are running the latest version"
            };
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, UpdateStatus.Error);
            return new UpdateCheckResult
            {
                UpdateAvailable = false,
                CurrentVersion = CurrentVersion,
                Message = $"Error checking for updates: {ex.Message}"
            };
        }
    }

    public async Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<double>? progress = null)
    {
        StatusChanged?.Invoke(this, UpdateStatus.Downloading);

        var tempPath = Path.Combine(Path.GetTempPath(), "MassSuite");
        Directory.CreateDirectory(tempPath);

        var fileName = $"MassSuite-{update.Version}.zip";
        var filePath = Path.Combine(tempPath, fileName);

        try
        {
            using var response = await _httpClient.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    progress?.Report((double)downloadedBytes / totalBytes * 100);
                }
            }

            StatusChanged?.Invoke(this, UpdateStatus.ReadyToInstall);
            return filePath;
        }
        catch
        {
            StatusChanged?.Invoke(this, UpdateStatus.Error);
            throw;
        }
    }

    public Task<bool> VerifyUpdateAsync(string filePath, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash))
            return Task.FromResult(true);

        try
        {
            using var sha256 = SHA256.Create();
            using var fileStream = File.OpenRead(filePath);
            var hash = BitConverter.ToString(sha256.ComputeHash(fileStream)).Replace("-", "");
            return Task.FromResult(hash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task ScheduleInstallAsync(string updateFilePath)
    {
        var updateMarkerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".update_pending");
        File.WriteAllText(updateMarkerPath, updateFilePath);
        return Task.CompletedTask;
    }

    private static int CompareVersions(string version1, string version2)
    {
        var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
        var v2Parts = version2.Split('.').Select(int.Parse).ToArray();

        for (int i = 0; i < Math.Max(v1Parts.Length, v2Parts.Length); i++)
        {
            var v1 = i < v1Parts.Length ? v1Parts[i] : 0;
            var v2 = i < v2Parts.Length ? v2Parts[i] : 0;

            if (v1 > v2) return 1;
            if (v1 < v2) return -1;
        }

        return 0;
    }

    private class GitHubRelease
    {
        public string TagName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool Prerelease { get; set; }
        public DateTime PublishedAt { get; set; }
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    private class GitHubAsset
    {
        public string Name { get; set; } = string.Empty;
        public string BrowserDownloadUrl { get; set; } = string.Empty;
        public long Size { get; set; }
    }
}
