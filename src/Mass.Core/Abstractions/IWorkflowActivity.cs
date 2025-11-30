namespace Mass.Core.Abstractions;

public interface IWorkflowActivity
{
    string ActivityName { get; }
    string Description { get; }
    Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default);
}

public sealed record ActivityContext(
    IReadOnlyDictionary<string, object> Inputs,
    IDictionary<string, object> Outputs,
    IServiceProvider Services);

public sealed record ActivityResult(
    ActivityStatus Status,
    string? Message = null,
    Exception? Exception = null,
    TimeSpan? Duration = null)
{
    public static ActivityResult Success(string? message = null, TimeSpan? duration = null) =>
        new(ActivityStatus.Success, message, null, duration);

    public static ActivityResult Failure(string message, Exception? exception = null, TimeSpan? duration = null) =>
        new(ActivityStatus.Failure, message, exception, duration);

    public static ActivityResult Skipped(string reason, TimeSpan? duration = null) =>
        new(ActivityStatus.Skipped, reason, null, duration);

    public static ActivityResult Retry(string reason, TimeSpan? duration = null) =>
        new(ActivityStatus.Retry, reason, null, duration);
}

public enum ActivityStatus
{
    Success,
    Failure,
    Skipped,
    Retry
}
