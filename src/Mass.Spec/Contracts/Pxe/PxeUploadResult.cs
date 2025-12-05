namespace Mass.Spec.Contracts.Pxe;

/// <summary>
/// Represents the result of a PXE file upload.
/// </summary>
public class PxeUploadResult
{
    /// <summary>
    /// Whether the upload was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The descriptor of the uploaded file.
    /// </summary>
    public PxeBootFileDescriptor? FileDescriptor { get; set; }

    /// <summary>
    /// Error message if the upload failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
