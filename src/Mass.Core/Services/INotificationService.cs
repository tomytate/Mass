namespace Mass.Core.Services;

public enum NotificationSeverity
{
    Info,
    Success,
    Warning,
    Error
}

public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationSeverity Severity { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public interface INotificationService
{
    void ShowNotification(string title, string message, NotificationSeverity severity = NotificationSeverity.Info);
    event EventHandler<Notification> NotificationReceived;
    IEnumerable<Notification> GetRecentNotifications(int count = 10);
    void ClearNotifications();
}
