using Mass.Core.Workflows;
using Mass.Spec.Exceptions;
using Mass.Spec.Contracts.Workflow; // Added missing using directive
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mass.Core.Tests.Exceptions;

public class ErrorModelTests
{
    [Fact]
    public void OperationException_FormatsMessage_WithContext()
    {
        // Arrange
        var error = new ErrorCode("test_error", "Failed: {Reason}", false);
        var context = new Dictionary<string, object?> { { "Reason", "Disk Full" } };

        // Act
        var ex = new OperationException(error, context);

        // Assert
        Assert.Equal("Failed: Disk Full", ex.Message);
        Assert.Equal(error, ex.Error);
        Assert.Equal("Disk Full", ex.Context["Reason"]);
    }

    [Fact]
    public void OperationException_FormatsMessage_WithoutContext()
    {
        // Arrange
        var error = new ErrorCode("test_error", "Failed: {Reason}", false);

        // Act
        var ex = new OperationException(error);

        // Assert
        Assert.Equal("Failed: {Reason}", ex.Message);
    }
}
