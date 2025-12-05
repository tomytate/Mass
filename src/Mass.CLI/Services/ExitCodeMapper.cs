using Mass.Spec.Exceptions;

namespace Mass.CLI.Services;

public static class ExitCodeMapper
{
    public static int Map(ErrorCode? error)
    {
        if (error == null) return ExitCodes.Success;

        return error.Code switch
        {
            "elevation_required" => ExitCodes.AccessDenied,
            "device_not_found" => ExitCodes.DriveNotFound,
            "io_error" => ExitCodes.GeneralError, // Or specific IO code
            "validation_failed" => ExitCodes.WorkflowValidationError,
            "unauthorized" => ExitCodes.AccessDenied,
            "timeout" => ExitCodes.TimeoutError,
            "cancelled" => ExitCodes.CancellationError,
            "plugin_error" => ExitCodes.PluginError,
            "workflow_error" => ExitCodes.WorkflowError,
            "step_failed" => ExitCodes.WorkflowError,
            _ => ExitCodes.GeneralError
        };
    }
}
