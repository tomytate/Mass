using System.Text.Json;
using Mass.Spec.Contracts.Config;
using Mass.Spec.Contracts.Logging;
using Mass.Spec.Contracts.Plugins;
using Mass.Spec.Contracts.Pxe;
using Mass.Spec.Contracts.Usb;
using Mass.Spec.Contracts.Workflow;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mass.Spec.Tests;

public class SerializationTests
{
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;

    public SerializationTests()
    {
        _yamlSerializer = new SerializerBuilder()
            .Build();
        _yamlDeserializer = new DeserializerBuilder()
            .Build();
    }

    [Fact]
    public void WorkflowDefinition_ShouldSerializeAndDeserialize()
    {
        var workflow = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Id = "step-1",
                    Action = "burn.iso",
                    Parameters = new Dictionary<string, object> { { "isoPath", "C:\\test.iso" } }
                }
            }
        };

        AssertSerialization(workflow);
    }

    [Fact]
    public void UsbJob_ShouldSerializeAndDeserialize()
    {
        var job = new UsbJob
        {
            Id = "job-1",
            ImagePath = "C:\\image.iso",
            TargetDeviceId = "PhysicalDrive1",
            Verify = true,
            PartitionScheme = "GPT",
            FileSystem = "NTFS",
            PersistenceSizeMB = 4096
        };

        AssertSerialization(job);
    }

    [Fact]
    public void AppSettings_ShouldSerializeAndDeserialize()
    {
        var settings = new AppSettings
        {
            PluginsDirectory = "custom-plugins",
            Pxe = new PxeSettings 
            { 
                DhcpBindAddress = "10.0.0.1",
                TftpPort = 6969,
                EnableDhcp = false
            },
            Usb = new UsbSettings
            {
                VerifyAfterBurn = false,
                DefaultPartitionScheme = "MBR"
            }
        };

        AssertSerialization(settings);
    }

    [Fact]
    public void PluginManifest_ShouldSerializeAndDeserialize()
    {
        var manifest = new PluginManifest
        {
            Id = "plugin.core",
            Name = "Core Plugin",
            Author = "Mass Team",
            EntryAssembly = "Mass.Core.dll",
            EntryType = "Mass.Core.Plugins.CorePlugin",
            Dependencies = new List<string> { "dep1", "dep2" },
            Capabilities = new List<string> { "cap1" },
            Permissions = new List<string> { "perm1" },
            Enabled = true
        };

        AssertSerialization(manifest);
    }

    [Fact]
    public void LogEntry_ShouldSerializeAndDeserialize()
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Error,
            Message = "Something went wrong",
            Source = "Mass.Core"
        };

        AssertSerialization(entry);
    }

    private void AssertSerialization<T>(T original)
    {
        try
        {
            // JSON Test
            var json = JsonSerializer.Serialize(original);
            var deserializedJson = JsonSerializer.Deserialize<T>(json);
            Assert.NotNull(deserializedJson);
            
            // YAML Test
            var yaml = _yamlSerializer.Serialize(original);
            var deserializedYaml = _yamlDeserializer.Deserialize<T>(yaml);
            Assert.NotNull(deserializedYaml);
        }
        catch (Exception ex)
        {
            File.WriteAllText("test_failure.txt", ex.ToString());
            throw;
        }
    }
}
