using System.IO.Compression;

namespace Mass.Core.Updates;

public class RollbackService : IRollbackService
{
    private readonly string _backupDirectory;
    private readonly string _applicationDirectory;

    public RollbackService()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite");
        _backupDirectory = Path.Combine(appData, "backups");
        _applicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task<bool> BackupCurrentVersionAsync()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"backup_{timestamp}.zip";
            var backupPath = Path.Combine(_backupDirectory, backupFileName);

            await Task.Run(() =>
            {
                ZipFile.CreateFromDirectory(_applicationDirectory, backupPath, CompressionLevel.Fastest, false);
            });

            await CleanupOldBackupsAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RestorePreviousVersionAsync()
    {
        try
        {
            var latestBackup = GetAvailableBackups().OrderByDescending(b => b).FirstOrDefault();
            if (string.IsNullOrEmpty(latestBackup))
                return false;

            var backupPath = Path.Combine(_backupDirectory, latestBackup);

            await Task.Run(() =>
            {
                var tempExtractPath = Path.Combine(Path.GetTempPath(), "MassSuite_Restore");
                if (Directory.Exists(tempExtractPath))
                    Directory.Delete(tempExtractPath, true);

                ZipFile.ExtractToDirectory(backupPath, tempExtractPath);

                foreach (var file in Directory.GetFiles(tempExtractPath, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(tempExtractPath, file);
                    var targetPath = Path.Combine(_applicationDirectory, relativePath);
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                    File.Copy(file, targetPath, true);
                }

                Directory.Delete(tempExtractPath, true);
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<string> GetAvailableBackups()
    {
        try
        {
            return Directory.GetFiles(_backupDirectory, "backup_*.zip")
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Cast<string>();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    public async Task CleanupOldBackupsAsync(int keepCount = 3)
    {
        await Task.Run(() =>
        {
            try
            {
                var backups = Directory.GetFiles(_backupDirectory, "backup_*.zip")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(keepCount);

                foreach (var backup in backups)
                {
                    backup.Delete();
                }
            }
            catch { }
        });
    }

    public bool HasBackup()
    {
        return GetAvailableBackups().Any();
    }
}
