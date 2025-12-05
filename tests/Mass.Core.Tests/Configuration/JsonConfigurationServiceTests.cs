using Mass.Core.Configuration;
using Mass.Core.Logging;
using Mass.Spec.Config;
using Xunit;

namespace Mass.Core.Tests.Configuration;

public class JsonConfigurationServiceTests : IDisposable
{
    private readonly string _tempConfigPath;
    private readonly JsonConfigurationService _service;

    public JsonConfigurationServiceTests()
    {
        _tempConfigPath = Path.GetTempFileName();
        var logger = new FileLogService(); // Use FileLogService instead of NullLogger
        _service = new JsonConfigurationService(logger, _tempConfigPath);
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistSettings()
    {
        // Arrange
        _service.Set("General.Language", "fr-FR");
        
        // Act
        await _service.SaveAsync();

        // Assert
        Assert.True(File.Exists(_tempConfigPath));
        var content = await File.ReadAllTextAsync(_tempConfigPath);
        Assert.Contains("fr-FR", content);
    }

    [Fact]
    public async Task LoadAsync_ShouldReadPersistedSettings()
    {
        // Arrange
        var json = @"{
            ""General"": {
                ""Language"": ""de-DE""
            }
        }";
        await File.WriteAllTextAsync(_tempConfigPath, json);

        // Act
        await _service.LoadAsync();
        var lang = _service.Get<string>("General.Language");

        // Assert
        Assert.Equal("de-DE", lang);
    }

    [Fact]
    public void Get_ShouldReturnFallback_WhenKeyMissing()
    {
        // Act
        var val = _service.Get<string>("NonExistent", "default");

        // Assert
        Assert.Equal("default", val);
    }

    public void Dispose()
    {
        if (File.Exists(_tempConfigPath))
        {
            File.Delete(_tempConfigPath);
        }
    }
}
