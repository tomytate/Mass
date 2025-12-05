namespace Mass.Spec.Exceptions;

/// <summary>
/// Canonical error codes for Mass Suite.
/// </summary>
public static class MassErrorCodes
{
    public static readonly ErrorCode GeneralError = new("general_error", "An unexpected error occurred: {Message}", false);
    public static readonly ErrorCode ElevationRequired = new("elevation_required", "Operation requires administrative privileges.", false);
    public static readonly ErrorCode DeviceNotFound = new("device_not_found", "Device '{DeviceId}' was not found.", false);
    public static readonly ErrorCode IoError = new("io_error", "I/O error occurred: {Message}", true);
    public static readonly ErrorCode ValidationFailed = new("validation_failed", "Validation failed: {Message}", false);
    public static readonly ErrorCode Unauthorized = new("unauthorized", "Access denied: {Message}", false);
    public static readonly ErrorCode Timeout = new("timeout", "Operation timed out after {Duration}.", true);
    public static readonly ErrorCode Cancelled = new("cancelled", "Operation was cancelled by user.", false);
    public static readonly ErrorCode PluginError = new("plugin_error", "Plugin error: {Message}", false);
    public static readonly ErrorCode WorkflowError = new("workflow_error", "Workflow execution failed: {Message}", false);
}
