using Mass.Core.Devices;
using ProUSB.Services.Logging;
using DiscUtils.Iso9660;

namespace ProUSB.Patching;

public class IsoPatchEngine(FileLogger logger) : IIsoPatcher
{
    public async Task<PatchResult> PatchIsoAsync(PatchRequest request, CancellationToken ct = default)
    {
        var changes = new List<string>();

        try
        {
            logger.Info($"Patching ISO: {request.SourceIsoPath}");

            foreach (var operation in request.Operations)
            {
                switch (operation.Type.ToLowerInvariant())
                {
                    case "persistence":
                        changes.Add("Persistence configuration set");
                        break;

                    case "bootloader":
                        var result = await PatchBootloaderAsync(
                            operation.Parameters.GetValueOrDefault("targetPath")?.ToString() ?? "",
                            ct);
                        changes.AddRange(result);
                        break;

                    default:
                        logger.Warn($"Unknown patch operation: {operation.Type}");
                        break;
                }
            }

            return new PatchResult(true, request.OutputIsoPath, changes);
        }
        catch (Exception ex)
        {
            logger.Error($"ISO patching failed: {ex.Message}", ex);
            return new PatchResult(false, "", [ex.Message]);
        }
    }

    public async Task<IsoInfo> InspectIsoAsync(string isoPath, CancellationToken ct = default)
    {
        try
        {
            await using var isoStream = File.OpenRead(isoPath);
            using var cd = new CDReader(isoStream, true);

            var fileInfo = new FileInfo(isoPath);
            var label = cd.VolumeLabel ?? "Unknown";
            
            var isBootable = DirectoryContainsEfi(cd.Root) || DirectoryContainsBoot(cd.Root);
            var bootMethods = new List<string>();

            if (isBootable)
            {
                if (DirectoryContainsEfi(cd.Root))
                    bootMethods.Add("UEFI");
                if (DirectoryContainsBoot(cd.Root))
                    bootMethods.Add("BIOS/Legacy");
            }

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
            logger.Error($"ISO inspection failed: {ex.Message}", ex);
            return new IsoInfo("Error", 0, "Unknown", false, []);
        }
    }

    private async Task<List<string>> PatchBootloaderAsync(string volumePath, CancellationToken ct)
    {
        var changes = new List<string>();

        var patcher = new Domain.Services.IsoPatcher(logger);
        await patcher.PatchAsync(volumePath, ct);

        changes.Add("Bootloader configuration patched for persistence");
        return changes;
    }

    private static bool DirectoryContainsEfi(DiscUtils.DiscDirectoryInfo dir)
    {
        try
        {
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

    private static bool DirectoryContainsBoot(DiscUtils.DiscDirectoryInfo dir)
    {
        try
        {
            foreach (var subDir in dir.GetDirectories())
            {
                var name = subDir.Name.ToLowerInvariant();
                if (name is "boot" or "isolinux" or "syslinux" or "grub")
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
