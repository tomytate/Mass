namespace Mass.Core.Updates;

public class UpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public long SizeBytes { get; set; }
    public bool IsPrerelease { get; set; }
}

public class UpdateCheckResult
{
    public bool UpdateAvailable { get; set; }
    public UpdateInfo? LatestVersion { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public enum UpdateStatus
{
    CheckingForUpdates,
    UpdateAvailable,
    Downloading,
    ReadyToInstall,
    Installing,
    UpToDate,
    Error
}
