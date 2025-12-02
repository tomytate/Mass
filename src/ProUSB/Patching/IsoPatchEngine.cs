using Mass.Core.Devices;
using ProUSB.Services.Logging;
using DiscUtils.Iso9660;

namespace ProUSB.Patching;

public class IsoPatchEngine : IIsoPatcher
{
    private readonly FileLogger _logger;

    public IsoPatchEngine(FileLogger logger)
    {
        _logger = logger;
    }

    public async Task<PatchResult> PatchIsoAsync(PatchRequest request, CancellationToken ct = default)
    {
        var changes = new List<string>();

        try
        {
            _logger.Info($"Patching ISO: {request.SourceIsoPath}");

            // For now, we'll implement live patching (patch files on mounted volume)
            // Future enhancement: Extract ISO, patch, rebuild ISO

            foreach (var operation in request.Operations)
            {
                switch (operation.Type.ToLowerInvariant())
                {
                    case "persistence":
                        // This is handled during burn, not ISO modification
                        changes.Add("Persistence configuration set");
                        break;

                    case "bootloader":
                        // Patch bootloader config files
                        var result = await PatchBootloaderAsync(
                            operation.Parameters.GetValueOrDefault("targetPath")?.ToString() ?? "",
                            ct);
                        changes.AddRange(result);
                        break;

                    default:
                        _logger.Warn($"Unknown patch operation: {operation.Type}");
                        break;
                }
            }

            return new PatchResult(true, request.OutputIsoPath, changes);
        }
        catch (Exception ex)
        {
            _logger.Error($"ISO patching failed: {ex.Message}", ex);
            return new PatchResult(false, "", new List<string> { ex.Message });
        }
    }

    public async Task<IsoInfo> InspectIsoAsync(string isoPath, CancellationToken ct = default)
    {
        try
        {
            using var isoStream = File.OpenRead(isoPath);
            using var cd = new CDReader(isoStream, true);

            var fileInfo = new FileInfo(isoPath);
            var label = cd.VolumeLabel ?? "Unknown";
            
            // Check for bootability by looking for EFI or boot directories
            var isBootable = DirectoryContainsEfi(cd.Root) || DirectoryContainsBoot(cd.Root);
            var bootMethods = new List<string>();

            if (isBootable)
            {
                if (DirectoryContainsEfi(cd.Root))
                    bootMethods.Add("UEFI");
                if (DirectoryContainsBoot(cd.Root))
                    bootMethods.Add("BIOS/Legacy");
            }

            await Task.CompletedTask; // Placeholder for async consistency

            return new IsoInfo(
                label,
                fileInfo.Length,
                "ISO9660",
                isBootable,
                bootMethods
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"ISO inspection failed: {ex.Message}", ex);
            return new IsoInfo("Error", 0, "Unknown", false, new List<string>());
        }
    }

    private async Task<List<string>> PatchBootloaderAsync(string volumePath, CancellationToken ct)
    {
        var changes = new List<string>();

        // Reuse logic from original IsoPatcher
        var patcher = new Domain.Services.IsoPatcher(_logger);
        await patcher.PatchAsync(volumePath, ct);

        changes.Add("Bootloader configuration patched for persistence");
        return changes;
    }

    private bool DirectoryContainsEfi(DiscUtils.DiscDirectoryInfo dir)
    {
        try
        {
            // Look for EFI directory or boot files
            foreach (var subDir in dir.GetDirectories())
            {
                if (subDir.Name.Equals("EFI", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (DirectoryContainsEfi(subDir))
                    return true;
            }

            foreach (var file in dir.GetFiles())
            {
                if (file.Name.EndsWith(".efi", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool DirectoryContainsBoot(DiscUtils.DiscDirectoryInfo dir)
    {
        try
        {
            // Look for common boot directories or files
            foreach (var subDir in dir.GetDirectories())
            {
                var name = subDir.Name.ToLowerInvariant();
                if (name == "boot" || name == "isolinux" || name == "syslinux" || name == "grub")
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
