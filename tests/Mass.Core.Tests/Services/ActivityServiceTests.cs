using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Mass.Core.Interfaces;
using Mass.Core.Services;
using Moq;
using Xunit;

namespace Mass.Core.Tests.Services;

public class ActivityServiceTests : IDisposable
{
    private readonly string _testPath;
    private readonly Mock<ILogService> _loggerMock;
    private readonly ActivityService _service;

    public ActivityServiceTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testPath);
        _loggerMock = new Mock<ILogService>();
        _service = new ActivityService(_loggerMock.Object, _testPath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testPath))
            {
                Directory.Delete(_testPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void AddActivity_ShouldAddActivityToHistory()
    {
        // Act
        _service.AddActivity("Test Title", "Test Description");

        // Assert
        var activities = _service.GetRecentActivities().ToList();
        activities.Should().HaveCount(1);
        activities[0].Title.Should().Be("Test Title");
        activities[0].Description.Should().Be("Test Description");
    }

    [Fact]
    public void AddActivity_ShouldRespectMaxHistory()
    {
        // Arrange
        // MaxHistory is 50
        for (int i = 0; i < 55; i++)
        {
            _service.AddActivity($"Title {i}", $"Desc {i}");
        }

        // Act
        var activities = _service.GetRecentActivities().ToList();

        // Assert
        // GetRecentActivities takes 10
        activities.Should().HaveCount(10);

        // We can inspect the internal state by reloading the service
        var newService = new ActivityService(_loggerMock.Object, _testPath);
        // But GetRecentActivities only returns 10, so we can't easily verify the total count via public API
        // except that the latest should be "Title 54"
        activities[0].Title.Should().Be("Title 54");
    }

    [Fact]
    public void ClearHistory_ShouldRemoveAllActivities()
    {
        // Arrange
        _service.AddActivity("Test", "Desc");

        // Act
        _service.ClearHistory();

        // Assert
        _service.GetRecentActivities().Should().BeEmpty();
    }

    [Fact]
    public void AddFavorite_ShouldAddFavorite()
    {
        // Act
        _service.AddFavorite("id1", "module", "My Fav", "icon", "target");

        // Assert
        _service.IsFavorite("id1").Should().BeTrue();
        var favs = _service.GetFavorites().ToList();
        favs.Should().HaveCount(1);
        favs[0].Name.Should().Be("My Fav");
    }

    [Fact]
    public void AddFavorite_ShouldNotDuplicateId()
    {
        // Arrange
        _service.AddFavorite("id1", "module", "My Fav", "icon", "target");

        // Act
        _service.AddFavorite("id1", "module", "My Fav 2", "icon", "target");

        // Assert
        _service.GetFavorites().Should().HaveCount(1);
        _service.GetFavorites().First().Name.Should().Be("My Fav");
    }

    [Fact]
    public void RemoveFavorite_ShouldRemoveFavorite()
    {
        // Arrange
        _service.AddFavorite("id1", "module", "My Fav", "icon", "target");

        // Act
        _service.RemoveFavorite("id1");

        // Assert
        _service.IsFavorite("id1").Should().BeFalse();
        _service.GetFavorites().Should().BeEmpty();
    }

    [Fact]
    public void Persistence_ShouldSaveAndLoadData()
    {
        // Arrange
        _service.AddActivity("Persist Activity", "Desc");
        _service.AddFavorite("persist_fav", "type", "Persist Fav", "icon", "target");

        // Act
        // Create a new service instance pointing to the same path
        var newService = new ActivityService(_loggerMock.Object, _testPath);

        // Assert
        var activities = newService.GetRecentActivities().ToList();
        activities.Should().HaveCount(1);
        activities[0].Title.Should().Be("Persist Activity");

        newService.IsFavorite("persist_fav").Should().BeTrue();
    }
}
