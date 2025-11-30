using System.Diagnostics;
using System.Security.Principal;

namespace Mass.Core.Security;

public class ElevationService : IElevationService
{
    public bool IsElevated
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            return false;
        }
    }

    public bool RequiresElevation(string operation)
    {
        var elevatedOperations = new[]
        {
            "disk_format",
            "disk_partition",
            "service_install",
            "driver_install",
            "system_modify"
        };

        return elevatedOperations.Contains(operation.ToLowerInvariant());
    }

    public Task<bool> RequestElevationAsync(string reason)
    {
        if (IsElevated)
            return Task.FromResult(true);

        return Task.FromResult(false);
    }

    public void RestartAsAdmin()
    {
        if (IsElevated)
            return;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty,
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch
        {
        }
    }
}
