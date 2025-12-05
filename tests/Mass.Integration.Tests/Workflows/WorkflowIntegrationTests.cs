using Microsoft.Extensions.DependencyInjection;
using Mass.Core.Workflows;
using Mass.Integration.Tests.Fixtures;
using Xunit;

namespace Mass.Integration.Tests.Workflows;

public class WorkflowIntegrationTests : IClassFixture<TestHostFixture>
{
    private readonly TestHostFixture _fixture;
    private readonly TestLogService _logService;
    
    public WorkflowIntegrationTests(TestHostFixture fixture)
    {
        _fixture = fixture;
        _logService = fixture.Services.GetRequiredService<Mass.Core.Interfaces.ILogService>() as TestLogService 
            ?? throw new InvalidOperationException("TestLogService not registered");
    }
    
    [Fact]
    public void WorkflowParser_ValidYaml_ParsesCorrectly()
    {
        // Arrange
        var yaml = @"
name: Test Workflow
version: 1.0
steps:
  - name: Step 1
    type: test
    config: {}
";
        var parser = new WorkflowParser();
        
        // Act
        var workflow = parser.ParseYaml(yaml);
        
        // Assert
        Assert.NotNull(workflow);
        Assert.Equal("Test Workflow", workflow.Name);
        Assert.Single(workflow.Steps);
    }
    
    [Fact]
    public void TestLogService_CapturesLogs()
    {
        // Arrange
        _logService.Logs.Clear();
        
        // Act
        _logService.LogInformation("Test message", "TestCategory");
        
        // Assert
        Assert.Single(_logService.Logs);
        Assert.Contains(_logService.Logs, l => l.Message == "Test message");
    }
}
