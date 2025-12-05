using Mass.Spec.Contracts.Usb;

namespace Mass.Core.Interfaces;

/// <summary>
/// Public facade for USB device burning operations.
/// </summary>
public interface IUsbBurner
{
    /// <summary>
    /// Burns an image to a USB device.
    /// </summary>
    /// <param name="job">The USB job describing the burn operation.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the burn operation.</returns>
    Task<BurnResult> BurnAsync(UsbJob job, IProgress<BurnProgress>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Lists all available USB devices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of detected USB devices.</returns>
    Task<IEnumerable<DeviceInfo>> ListDevicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Verifies a USB device after burning.
    /// </summary>
    /// <param name="job">The USB job to verify.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the verification.</returns>
    Task<VerifyResult> VerifyAsync(UsbJob job, CancellationToken ct = default);
}
