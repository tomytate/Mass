using Mass.Spec.Contracts.Pxe;

namespace Mass.Core.Interfaces;

/// <summary>
/// Public facade for PXE boot file management.
/// </summary>
public interface IPxeManager
{
    /// <summary>
    /// Uploads a boot file to the PXE server.
    /// </summary>
    /// <param name="file">The boot file descriptor to upload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the upload operation.</returns>
    Task<PxeUploadResult> UploadBootFileAsync(PxeBootFileDescriptor file, CancellationToken ct = default);

    /// <summary>
    /// Lists all boot files available on the PXE server.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of boot file descriptors.</returns>
    Task<IEnumerable<PxeBootFileDescriptor>> ListBootFilesAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes a boot file from the PXE server.
    /// </summary>
    /// <param name="id">The unique identifier of the boot file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the file was deleted successfully.</returns>
    Task<bool> DeleteBootFileAsync(string id, CancellationToken ct = default);
}
