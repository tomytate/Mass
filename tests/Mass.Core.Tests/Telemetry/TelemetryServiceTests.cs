using Mass.Core.Interfaces;
using Mass.Core.Telemetry;
using Mass.Spec.Contracts.Logging;
using Moq;
using Xunit;

namespace Mass.Core.Tests.Telemetry;

public class TelemetryServiceTests : IDisposable
{
    private readonly string _tempLogDir;
    private readonly Mock<IConfigurationService> _mockConfig;
    private readonly LocalTelemetryService _telemetryService;

    public TelemetryServiceTests()
    {
        _tempLogDir = Path.Combine(Path.GetTempPath(), $"telemetry_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempLogDir);

        _mockConfig = new Mock<IConfigurationService>();
        _mockConfig.Setup(c => c.Get("Telemetry.ConsentDecisionMade", false)).Returns(false);
        _mockConfig.Setup(c => c.Get("Telemetry.Enabled", false)).Returns(false);

        _telemetryService = new LocalTelemetryService(_mockConfig.Object, _tempLogDir);
    }

    [Fact]
    public async Task TrackEvent_WithConsentGiven_WritesToFile()
    {
        // Arrange
        _mockConfig.Setup(c => c.Get("Telemetry.ConsentDecisionMade", false)).Returns(true);
        _mockConfig.Setup(c => c.Get("Telemetry.Enabled", false)).Returns(true);

        var telemetryEvent = new TelemetryEvent
        {
            EventType = "Test",
            Source = "UnitTest",
            Name = "TestEvent",
            Properties = new Dictionary<string, object> { ["Key"] = "Value" }
        };

        // Act
        _telemetryService.TrackEvent(telemetryEvent);
        await _telemetryService.FlushAsync();

        // Assert
        var logFiles = Directory.GetFiles(_tempLogDir, "telemetry_*.json");
        Assert.NotEmpty(logFiles);

        var content = await File.ReadAllTextAsync(logFiles[0]);
        Assert.Contains("TestEvent", content);
        Assert.Contains("UnitTest", content);
    }

    [Fact]
    public async Task TrackEvent_WithoutConsent_DoesNotWrite()
    {
        // Arrange
        _mockConfig.Setup(c => c.Get("Telemetry.ConsentDecisionMade", false)).Returns(false);
        _mockConfig.Setup(c => c.Get("Telemetry.Enabled", false)).Returns(false);

        var telemetryEvent = new TelemetryEvent
        {
            EventType = "Test",
            Source = "UnitTest",
            Name = "TestEvent",
            Properties = new Dictionary<string, object> { ["Key"] = "Value" }
        };

        // Act
        _telemetryService.TrackEvent(telemetryEvent);
        await _telemetryService.FlushAsync();

        // Assert
        var logFiles = Directory.GetFiles(_tempLogDir, "telemetry_*.json");
        Assert.Empty(logFiles);
    }

    [Fact]
    public async Task TrackEvent_SerializesCorrectly()
    {
        // Arrange
        _mockConfig.Setup(c => c.Get("Telemetry.ConsentDecisionMade", false)).Returns(true);
        _mockConfig.Setup(c => c.Get("Telemetry.Enabled", false)).Returns(true);

        var telemetryEvent = new TelemetryEvent
        {
            EventType = "Event",
            Source = "TestSource",
            Name = "TestName",
            Properties = new Dictionary<string, object> 
            { 
                ["StringProp"] = "value",
                ["IntProp"] = 42
            },
            Metrics = new Dictionary<string, double>
            {
                ["Duration"] = 123.45
            }
        };

        // Act
        _telemetryService.TrackEvent(telemetryEvent);
        await _telemetryService.FlushAsync();

        // Assert
        var logFiles = Directory.GetFiles(_tempLogDir, "telemetry_*.json");
        var content = await File.ReadAllTextAsync(logFiles[0]);
        
        Assert.Contains("\"EventType\":\"Event\"", content);
        Assert.Contains("\"Source\":\"TestSource\"", content);
        Assert.Contains("\"Name\":\"TestName\"", content);
        Assert.Contains("StringProp", content);
        Assert.Contains("Duration", content);
    }

    [Fact]
    public async Task FlushAsync_WritesBufferedEvents()
    {
        // Arrange
        _mockConfig.Setup(c => c.Get("Telemetry.ConsentDecisionMade", false)).Returns(true);
        _mockConfig.Setup(c => c.Get("Telemetry.Enabled", false)).Returns(true);

        for (int i = 0; i < 5; i++)
        {
            _telemetryService.TrackEvent(new TelemetryEvent
            {
                EventType = "Event",
                Source = "Test",
                Name = $"Event{i}",
                Properties = new Dictionary<string, object>()
            });
        }

        // Act
        await _telemetryService.FlushAsync();

        // Assert
        var logFiles = Directory.GetFiles(_tempLogDir, "telemetry_*.json");
        var lines = await File.ReadAllLinesAsync(logFiles[0]);
        Assert.Equal(5, lines.Length);
    }

    [Fact]
    public void ConsentGiven_PersistsToConfiguration()
    {
        // Act
        _telemetryService.ConsentGiven = true;

        // Assert
        _mockConfig.Verify(c => c.Set("Telemetry.ConsentDecisionMade", true), Times.Once);
        _mockConfig.Verify(c => c.Set("Telemetry.Enabled", true), Times.Once);

        // Act
        _telemetryService.ConsentGiven = false;

        // Assert
        _mockConfig.Verify(c => c.Set("Telemetry.ConsentDecisionMade", true), Times.Exactly(2));
        _mockConfig.Verify(c => c.Set("Telemetry.Enabled", false), Times.Once);
    }

    [Fact]
    public void TrackEvent_SanitizesUserPaths()
    {
        // Arrange
        _mockConfig.Setup(c => c.Get("Telemetry.ConsentDecisionMade", false)).Returns(true);
        _mockConfig.Setup(c => c.Get("Telemetry.Enabled", false)).Returns(true);

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var testPath = Path.Combine(userProfile, "Documents", "test.txt");

        var telemetryEvent = new TelemetryEvent
        {
            EventType = "Event",
            Source = "Test",
            Name = "PathTest",
            Properties = new Dictionary<string, object> { ["FilePath"] = testPath }
        };

        // Act
        _telemetryService.TrackEvent(telemetryEvent);

        // Assert - event should have sanitized path
        Assert.Contains("%USERPROFILE%", telemetryEvent.Properties["FilePath"].ToString()!);
        Assert.DoesNotContain(userProfile, telemetryEvent.Properties["FilePath"].ToString()!);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempLogDir))
        {
            Directory.Delete(_tempLogDir, recursive: true);
        }
    }
}
