namespace Mass.Core.Domain.Models;

public sealed class UsbJob : Entity<Guid>
{
    public string ImagePath { get; private set; }
    public string TargetDrive { get; private set; }
    public UsbJobStatus Status { get; private set; }
    public long TotalBytes { get; private set; }
    public long ProcessedBytes { get; private set; }
    public int ProgressPercentage => TotalBytes > 0 ? (int)((ProcessedBytes * 100) / TotalBytes) : 0;
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private UsbJob(Guid id, string imagePath, string targetDrive, long totalBytes) : base(id)
    {
        ImagePath = imagePath;
        TargetDrive = targetDrive;
        TotalBytes = totalBytes;
        ProcessedBytes = 0;
        Status = UsbJobStatus.Pending;
    }

    public static UsbJob Create(string imagePath, string targetDrive, long totalBytes)
    {
        var job = new UsbJob(Guid.NewGuid(), imagePath, targetDrive, totalBytes);
        job.RaiseDomainEvent(new UsbJobCreatedEvent(job.Id, imagePath, targetDrive));
        return job;
    }

    public void Start()
    {
        Status = UsbJobStatus.Running;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UsbJobStartedEvent(Id));
    }

    public void UpdateProgress(long processedBytes)
    {
        if (processedBytes < 0 || processedBytes > TotalBytes)
            throw new ArgumentOutOfRangeException(nameof(processedBytes));

        ProcessedBytes = processedBytes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = UsbJobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ProcessedBytes = TotalBytes;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UsbJobCompletedEvent(Id));
    }

    public void Fail(string errorMessage)
    {
        Status = UsbJobStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UsbJobFailedEvent(Id, errorMessage));
    }
}

public enum UsbJobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public sealed class UsbJobCreatedEvent : DomainEvent
{
    public Guid JobId { get; }
    public string ImagePath { get; }
    public string TargetDrive { get; }

    public UsbJobCreatedEvent(Guid jobId, string imagePath, string targetDrive)
    {
        JobId = jobId;
        ImagePath = imagePath;
        TargetDrive = targetDrive;
    }
}

public sealed class UsbJobStartedEvent : DomainEvent
{
    public Guid JobId { get; }

    public UsbJobStartedEvent(Guid jobId)
    {
        JobId = jobId;
    }
}

public sealed class UsbJobCompletedEvent : DomainEvent
{
    public Guid JobId { get; }

    public UsbJobCompletedEvent(Guid jobId)
    {
        JobId = jobId;
    }
}

public sealed class UsbJobFailedEvent : DomainEvent
{
    public Guid JobId { get; }
    public string ErrorMessage { get; }

    public UsbJobFailedEvent(Guid jobId, string errorMessage)
    {
        JobId = jobId;
        ErrorMessage = errorMessage;
    }
}
