using FluentAssertions;
using Mass.Core;
using Mass.Core.Configuration;
using Xunit;

namespace Mass.Core.Tests.Configuration;

public class ConfigurationManagerTests : IDisposable
{
    private readonly string _testConfigPath;

    public ConfigurationManagerTests()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"test-config-{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_testConfigPath))
            File.Delete(_testConfigPath);
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_CreatesDefaultConfiguration()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);

        // Act
        await manager.LoadAsync();

        // Assert
        manager.Current.Should().NotBeNull();
        manager.Current.App.Should().NotBeNull();
        manager.Current.Usb.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveAsync_PersistsConfiguration()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);
        await manager.LoadAsync();

        manager.Update(c =>
        {
            c.App.Theme = "Dark";
            c.App.Language = "en-US";
        });

        // Act
        await manager.SaveAsync();

        // Assert
        File.Exists(_testConfigPath).Should().BeTrue();
        
        var content = await File.ReadAllTextAsync(_testConfigPath);
        content.Should().Contain("Dark");
        content.Should().Contain("en-US");
    }

    [Fact]
    public async Task LoadAsync_AfterSave_RestoresConfiguration()
    {
        // Arrange
        var manager1 = new ConfigurationManager(_testConfigPath);
        await manager1.LoadAsync();
        manager1.Update(c => c.App.Theme = "Light");
        await manager1.SaveAsync();

        // Act
        var manager2 = new ConfigurationManager(_testConfigPath);
        await manager2.LoadAsync();

        // Assert
        manager2.Current.App.Theme.Should().Be("Light");
    }

    [Fact]
    public void Update_ModifiesConfiguration()
    {
        // Arrange
        var manager = new ConfigurationManager(_testConfigPath);
        manager.LoadAsync().GetAwaiter().GetResult();
        var originalTheme = manager.Current.App.Theme;

        // Act
        manager.Update(c => c.App.Theme = "Custom");

        // Assert
        manager.Current.App.Theme.Should().Be("Custom");
        manager.Current.App.Theme.Should().NotBe(originalTheme);
    }
}
