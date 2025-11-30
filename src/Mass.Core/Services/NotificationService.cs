using System.Collections.Concurrent;

namespace Mass.Core.Services;

public class NotificationService : INotificationService
{
    private readonly ConcurrentQueue<Notification> _notifications = new();
    private const int MaxNotifications = 50;

    public event EventHandler<Notification>? NotificationReceived;

    public void ShowNotification(string title, string message, NotificationSeverity severity = NotificationSeverity.Info)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Severity = severity,
            Timestamp = DateTime.Now
        };

        _notifications.Enqueue(notification);
        
        while (_notifications.Count > MaxNotifications)
        {
            _notifications.TryDequeue(out _);
        }

        NotificationReceived?.Invoke(this, notification);
    }

    public IEnumerable<Notification> GetRecentNotifications(int count = 10)
    {
        return _notifications.Reverse().Take(count);
    }

    public void ClearNotifications()
    {
        _notifications.Clear();
    }
}
