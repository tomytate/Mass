using Mass.Core.Interfaces;
using Mass.Spec.Contracts.Pxe;
using ProPXEServer.API.Data;

namespace ProPXEServer.API.Services;

/// <summary>
/// Implementation of IPxeManager for ProPXEServer.
/// </summary>
public class PxeManager : IPxeManager
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PxeManager> _logger;
    private readonly string _pxeRoot;

    public PxeManager(
        ApplicationDbContext db,
        ILogger<PxeManager> logger,
        IConfiguration config,
        string? pxeRootPath = null)
    {
        _db = db;
        _logger = logger;
        _pxeRoot = pxeRootPath ?? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pxe"));
    }

    public async Task<PxeUploadResult> UploadBootFileAsync(
        PxeBootFileDescriptor file, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Uploading boot file: {FileName}", file.FileName);

            var architecture = file.Architecture.ToLower();
            var targetDir = Path.Combine(_pxeRoot, architecture);
            
            Directory.CreateDirectory(targetDir);
            
            var filePath = Path.Combine(targetDir, file.FileName);

            // Validate path to prevent traversal
            var fullPath = Path.GetFullPath(filePath);

            // Ensure trailing separator for root to prevent partial match issues (e.g. /root matching /root-sibling)
            var rootPathWithSeparator = _pxeRoot.EndsWith(Path.DirectorySeparatorChar) ? _pxeRoot : _pxeRoot + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(rootPathWithSeparator, StringComparison.OrdinalIgnoreCase) &&
                !fullPath.Equals(_pxeRoot, StringComparison.OrdinalIgnoreCase)) // Allow root itself if needed, though rare for file upload
            {
                return new PxeUploadResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid file path"
                };
            }

            // In real implementation, file.Data would contain the actual file bytes
            // For now, create an empty file as placeholder
            await File.WriteAllBytesAsync(filePath, Array.Empty<byte>(), ct);

            _logger.LogInformation("Successfully uploaded boot file: {FileName} to {Architecture}", 
                file.FileName, architecture);

            return new PxeUploadResult
            {
                IsSuccess = true,
                FileDescriptor = new PxeBootFileDescriptor 
                { 
                    RelativePath = Path.GetRelativePath(_pxeRoot, filePath).Replace('\\', '/'),
                    FileName = file.FileName,
                    Architecture = architecture
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading boot file: {FileName}", file.FileName);
            return new PxeUploadResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<IEnumerable<PxeBootFileDescriptor>> ListBootFilesAsync(CancellationToken ct = default)
    {
        try
        {
            var descriptors = new List<PxeBootFileDescriptor>();

            if (!Directory.Exists(_pxeRoot))
            {
                return descriptors;
            }

            var files = Directory.GetFiles(_pxeRoot, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var relativePath = Path.GetRelativePath(_pxeRoot, file);
                var architecture = GetArchitectureFromPath(file);

                descriptors.Add(new PxeBootFileDescriptor
                {
                    Id = descriptors.Count.ToString(),
                    FileName = Path.GetFileName(file),
                    RelativePath = relativePath.Replace('\\', '/'),
                    Architecture = architecture,
                    Size = fileInfo.Length
                });
            }

            await Task.CompletedTask;
            return descriptors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing boot files");
            return Enumerable.Empty<PxeBootFileDescriptor>();
        }
    }

    public async Task<bool> DeleteBootFileAsync(string id, CancellationToken ct = default)
    {
        try
        {
            // In a real implementation, id would map to a specific file
            // For now, treat id as relative path
            var filePath = Path.GetFullPath(Path.Combine(_pxeRoot, id));

            if (!filePath.StartsWith(_pxeRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt in delete: {Id}", id);
                return false;
            }

            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);
            _logger.LogInformation("Deleted boot file: {Id}", id);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting boot file: {Id}", id);
            return false;
        }
    }

    private static string GetArchitectureFromPath(string filePath)
    {
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        var directory = Path.GetDirectoryName(filePath)?.Split(Path.DirectorySeparatorChar).LastOrDefault()?.ToLowerInvariant();

        if (directory == "arm64" || fileName.Contains("arm64")) 
            return "ARM64";
        if (directory == "uefi" || fileName.EndsWith(".efi")) 
            return "x64_UEFI";
        if (directory == "bios" || fileName.EndsWith(".kpxe") || fileName.EndsWith(".pxe")) 
            return "x86_BIOS";

        return "x86_BIOS";
    }
}
