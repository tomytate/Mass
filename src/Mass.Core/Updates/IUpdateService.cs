namespace Mass.Core.Updates;

public interface IUpdateService
{
    string CurrentVersion { get; }
    Task<UpdateCheckResult> CheckForUpdatesAsync();
    Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<double>? progress = null);
    Task<bool> VerifyUpdateAsync(string filePath, string expectedHash);
    Task ScheduleInstallAsync(string updateFilePath);
    event EventHandler<UpdateStatus> StatusChanged;
}
