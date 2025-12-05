using Mass.Core.Registry;
using Mass.Spec.Contracts.Plugins;
using Xunit;

namespace Mass.Core.Tests.Registry;

public class RegistryServiceTests
{
    [Fact]
    public void RegisterStep_ShouldStoreStep()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var registry = new RegistryService(serviceProvider);
        var step = new StepDescriptor
        {
            Id = "test.step",
            Version = "1.0.0",
            HandlerTypeName = "Test.Handler",
            Description = "Test step"
        };

        // Act
        registry.RegisterStep(step);
        var found = registry.FindStep("test.step");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("test.step", found.Id);
        Assert.Equal("1.0.0", found.Version);
    }

    [Fact]
    public void RegisterStep_WithEmptyId_ShouldThrow()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var registry = new RegistryService(serviceProvider);
        var step = new StepDescriptor { Id = "" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => registry.RegisterStep(step));
    }

    [Fact]
    public void FindStep_WithUnknownId_ShouldReturnNull()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var registry = new RegistryService(serviceProvider);

        // Act
        var found = registry.FindStep("unknown");

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void ListSteps_ShouldReturnAllSteps()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var registry = new RegistryService(serviceProvider);
        registry.RegisterStep(new StepDescriptor { Id = "step1" });
        registry.RegisterStep(new StepDescriptor { Id = "step2" });

        // Act
        var steps = registry.ListSteps();

        // Assert
        Assert.Equal(2, steps.Count);
    }

    [Fact]
    public void RegisterStep_ShouldBeThreadSafe()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var registry = new RegistryService(serviceProvider);
        var tasks = new List<Task>();
        var stepCount = 100;

        // Act
        for (int i = 0; i < stepCount; i++)
        {
            var stepId = $"step{i}";
            tasks.Add(Task.Run(() =>
            {
                registry.RegisterStep(new StepDescriptor { Id = stepId });
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var steps = registry.ListSteps();
        Assert.Equal(stepCount, steps.Count);
    }

    [Fact]
    public void RegisterPlugin_ShouldStorePlugin()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var registry = new RegistryService(serviceProvider);
        var plugin = new LoadedPluginDescriptor
        {
            Id = "test.plugin",
            Manifest = new PluginManifest
            {
                Id = "test.plugin",
                Name = "Test Plugin",
                Version = "1.0.0"
            },
            State = PluginState.Active
        };

        // Act
        registry.RegisterPlugin(plugin);

        // Assert
        Assert.Single(registry.LoadedPlugins);
        Assert.True(registry.LoadedPlugins.ContainsKey("test.plugin"));
    }

    [Fact]
    public void RegisterPlugin_WithEmptyId_ShouldThrow()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var registry = new RegistryService(serviceProvider);
        var plugin = new LoadedPluginDescriptor { Id = "" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => registry.RegisterPlugin(plugin));
    }

    [Fact]
    public void Save_And_Load_ShouldPersistMetadata()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var registry1 = new RegistryService(serviceProvider);
        
        registry1.RegisterStep(new StepDescriptor
        {
            Id = "persist.test",
            Version = "2.0.0",
            HandlerTypeName = "Test.PersistHandler",
            Permissions = new[] { "perm1", "perm2" },
            Description = "Persistence test"
        });

        registry1.RegisterPlugin(new LoadedPluginDescriptor
        {
            Id = "persist.plugin",
            Manifest = new PluginManifest
            {
                Id = "persist.plugin",
                Name = "Persist Plugin"
            },
            State = PluginState.Loaded
        });

        // Act - Save
        registry1.Save();

        // Create new registry instance and load
        var registry2 = new RegistryService(serviceProvider);
        registry2.Load();

        // Assert
        var step = registry2.FindStep("persist.test");
        Assert.NotNull(step);
        Assert.Equal("2.0.0", step.Version);
        Assert.Equal(2, step.Permissions.Length);
        Assert.Equal("perm1", step.Permissions[0]);

        Assert.True(registry2.LoadedPlugins.ContainsKey("persist.plugin"));
        var plugin = registry2.LoadedPlugins["persist.plugin"];
        Assert.Equal(PluginState.Loaded, plugin.State);
    }

    [Fact]
    public void ResolveHandler_ShouldReturnInstanceFromServiceProvider()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var testHandler = new TestHandler();
        serviceProvider.Register(testHandler);

        var registry = new RegistryService(serviceProvider);

        // Act
        var resolved = registry.ResolveHandler(typeof(TestHandler));

        // Assert
        Assert.NotNull(resolved);
        Assert.Same(testHandler, resolved);
    }

    [Fact]
    public void ResolveHandler_WithUnknownType_ShouldReturnNull()
    {
        // Arrange
        var serviceProvider = new TestServiceProvider();
        var registry = new RegistryService(serviceProvider);

        // Act
        var resolved = registry.ResolveHandler(typeof(UnknownHandler));

        // Assert
        Assert.Null(resolved);
    }

    // Test classes
    private class TestHandler { }
    private class UnknownHandler { }

    private class TestServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<T>(T instance) where T : notnull
        {
            _services[typeof(T)] = instance;
        }

        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var service) ? service : null;
        }
    }
}
