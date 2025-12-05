using Mass.Core.Workflows;
using Mass.Spec.Contracts.Workflow;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Mass.Core.Tests.Workflows;

public class WorkflowTests
{
    [Fact]
    public void Parser_ShouldParseValidYaml()
    {
        var parser = new WorkflowParser();
        var yaml = @"
id: test-workflow
name: Test Workflow
steps:
  - id: step1
    name: Step 1
    type: Command
    parameters:
      command: echo hello
";
        var workflow = parser.ParseYaml(yaml);

        Assert.Equal("test-workflow", workflow.Id);
        Assert.Single(workflow.Steps);
        Assert.Equal("Command", workflow.Steps[0].Action);
    }

    [Fact]
    public void Validator_ShouldDetectMissingId()
    {
        var validator = new WorkflowValidator();
        var workflow = new WorkflowDefinition { Name = "Test" };
        
        var result = validator.Validate(workflow);

        Assert.False(result.IsValid);
        Assert.Contains("Workflow ID is required", result.Errors);
    }

    [Fact]
    public async Task Executor_ShouldRunCommandStep()
    {
        var executor = new WorkflowExecutor(new Mass.Core.Logging.FileLogService());
        var workflow = new WorkflowDefinition
        {
            Id = "exec-test",
            Name = "Execution Test",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Id = "s1",
                    Name = "Echo",
                    Action = "Command",
                    Parameters = new Dictionary<string, object>
                    {
                        { "command", "echo test_output" }
                    }
                }
            }
        };

        var result = await executor.ExecuteAsync(workflow);

        Assert.True(result.Success);
        // Note: StepResults values might be objects, so ToString() is needed or cast
        var stepResult = result.CompletedSteps.FirstOrDefault(s => s.StepId == "s1");
        Assert.NotNull(stepResult);
        Assert.Contains("test_output", stepResult.Output?.ToString());
    }
}
