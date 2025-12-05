using Mass.Spec.Contracts.Usb;

namespace Mass.Core.Devices;

public interface IUsbBurner
{
    Task<BurnResult> BurnIsoAsync(UsbJob job, IProgress<BurnProgress> progress, CancellationToken ct = default);
    Task<VerifyResult> VerifyAsync(string devicePath, CancellationToken ct = default);
    Task<bool> CancelAsync();
}

