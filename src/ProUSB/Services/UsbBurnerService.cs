using System.Diagnostics;

namespace ProUSB.Services;

public class UsbBurnerService : IUsbBurnerService
{
    public async Task BurnIsoAsync(string isoPath, string driveLetter, string fileSystem, string partitionScheme, IProgress<double> progress, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would contain the actual DiskPart / formatting logic
        // For now, we simulate the process to ensure the architecture is correct
        
        if (!File.Exists(isoPath))
            throw new FileNotFoundException($"ISO file not found: {isoPath}");

        // Simulate formatting
        for (int i = 0; i <= 30; i++)
        {
            if (cancellationToken.IsCancellationRequested) return;
            progress.Report(i);
            await Task.Delay(50, cancellationToken);
        }

        // Simulate copying
        for (int i = 31; i <= 90; i++)
        {
            if (cancellationToken.IsCancellationRequested) return;
            progress.Report(i);
            await Task.Delay(30, cancellationToken);
        }

        // Simulate verification
        for (int i = 91; i <= 100; i++)
        {
            if (cancellationToken.IsCancellationRequested) return;
            progress.Report(i);
            await Task.Delay(20, cancellationToken);
        }
    }
}
