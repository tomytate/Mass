namespace Mass.Core.Updates;

public interface IRollbackService
{
    Task<bool> BackupCurrentVersionAsync();
    Task<bool> RestorePreviousVersionAsync();
    IEnumerable<string> GetAvailableBackups();
    Task CleanupOldBackupsAsync(int keepCount = 3);
    bool HasBackup();
}
