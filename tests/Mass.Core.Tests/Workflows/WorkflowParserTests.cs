using FluentAssertions;
using Mass.Core.Workflows;
using Xunit;

namespace Mass.Core.Tests.Workflows;

public class WorkflowParserTests
{
    private readonly WorkflowParser _parser;

    public WorkflowParserTests()
    {
        _parser = new WorkflowParser();
    }

    [Fact]
    public async Task ParseFromFile_WithValidYaml_ReturnsWorkflowDefinition()
    {
        // Arrange
        var yamlContent = @"
id: test-workflow
name: Test Workflow
description: A test workflow
version: 1.0.0
parameters:
  param1: value1
  
steps:
  - id: step1
    name: First Step
    type: Command
    parameters:
      command: echo 'Hello'
";
        var tempFile = Path.GetTempFileName();
        var yamlFile = Path.ChangeExtension(tempFile, ".yaml");
        await File.WriteAllTextAsync(yamlFile, yamlContent);

        try
        {
            // Act
            var workflow = await _parser.ParseFromFileAsync(yamlFile);

            // Assert
            workflow.Should().NotBeNull();
            workflow.Id.Should().Be("test-workflow");
            workflow.Name.Should().Be("Test Workflow");
            workflow.Description.Should().Be("A test workflow");
            workflow.Steps.Should().HaveCount(1);
            workflow.Steps[0].Id.Should().Be("step1");
            workflow.Steps[0].Action.Should().Be("Command");
        }
        finally
        {
            if (File.Exists(yamlFile)) File.Delete(yamlFile);
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseFromFile_WithBurnStep_CreatesBurnStepType()
    {
        // Arrange
        var yamlContent = @"
id: burn-test
name: Burn Test
steps:
  - id: burn1
    type: Burn
    parameters:
      isoPath: test.iso
";
        var tempFile = Path.GetTempFileName();
        var yamlFile = Path.ChangeExtension(tempFile, ".yaml");
        await File.WriteAllTextAsync(yamlFile, yamlContent);

        try
        {
            // Act
            var workflow = await _parser.ParseFromFileAsync(yamlFile);

            // Assert
            // Note: In the new parser, we map to generic WorkflowStep with Action property
            workflow.Steps[0].Action.Should().Be("Burn");
            workflow.Steps[0].Id.Should().Be("burn1");
        }
        finally
        {
            if (File.Exists(yamlFile)) File.Delete(yamlFile);
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseFromFile_WithInvalidFile_ThrowsException()
    {
        // Arrange
        var invalidPath = "nonexistent-file.yaml";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _parser.ParseFromFileAsync(invalidPath));
    }
}
