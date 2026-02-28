using System;
using System.Linq;
using FluentAssertions;
using Mass.Core.Services;
using Xunit;

namespace Mass.Core.Tests.Services;

public class NotificationServiceTests
{
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _service = new NotificationService();
    }

    [Fact]
    public void ShowNotification_ShouldAddNotification()
    {
        // Act
        _service.ShowNotification("Test", "Message");

        // Assert
        var notifications = _service.GetRecentNotifications().ToList();
        notifications.Should().HaveCount(1);
        notifications[0].Title.Should().Be("Test");
        notifications[0].Message.Should().Be("Message");
    }

    [Fact]
    public void ShowNotification_ShouldRaiseEvent()
    {
        // Arrange
        Notification? receivedNotification = null;
        _service.NotificationReceived += (sender, n) => receivedNotification = n;

        // Act
        _service.ShowNotification("Test", "Message");

        // Assert
        receivedNotification.Should().NotBeNull();
        receivedNotification!.Title.Should().Be("Test");
    }

    [Fact]
    public void ShowNotification_ShouldRespectMaxCount()
    {
        // Arrange
        for (int i = 0; i < 55; i++)
        {
            _service.ShowNotification($"Title {i}", "Msg");
        }

        // Act
        // GetRecentNotifications returns 10 by default, check internal state indirectly or if API exposed
        // Since GetRecentNotifications takes a count, let's ask for 100
        var notifications = _service.GetRecentNotifications(100).ToList();

        // Assert
        // Implementation says MaxNotifications = 50
        notifications.Count.Should().BeLessThanOrEqualTo(50);
        notifications.First().Title.Should().Be("Title 54"); // Reverse order
    }

    [Fact]
    public void ClearNotifications_ShouldRemoveAll()
    {
        // Arrange
        _service.ShowNotification("Test", "Msg");

        // Act
        _service.ClearNotifications();

        // Assert
        _service.GetRecentNotifications().Should().BeEmpty();
    }
}
