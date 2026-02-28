namespace ProUSB.Services.IsoCreation;

using ProUSB.Domain;

public interface IIsoCreationService {
    Task<IsoCreationResult> CreateIsoFromDeviceAsync(
        UsbDeviceInfo device,
        string outputPath,
        IsoCreationMode mode,
        IProgress<IsoCreationProgress> progress,
        CancellationToken ct
    );
}
