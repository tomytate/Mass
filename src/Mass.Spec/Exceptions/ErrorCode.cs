namespace Mass.Spec.Exceptions;

/// <summary>
/// Represents a standardized error code definition.
/// </summary>
/// <param name="Code">The unique machine-readable error code (e.g., "device_not_found").</param>
/// <param name="MessageTemplate">A user-friendly message template (e.g., "Device '{DeviceId}' was not found.").</param>
/// <param name="IsRetryable">Indicates if the operation might succeed if retried.</param>
public record ErrorCode(string Code, string MessageTemplate, bool IsRetryable = false);
