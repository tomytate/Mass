namespace ProUSB.Services;

public interface IUsbBurnerService
{
    Task BurnIsoAsync(string isoPath, string driveLetter, string fileSystem, string partitionScheme, IProgress<double> progress, CancellationToken cancellationToken = default);
}
