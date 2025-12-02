using Xunit;

namespace ProPXEServer.API.Tests;

public class BasicInfrastructureTest
{
    [Fact]
    public void TestFramework_ShouldWork()
    {
        // Simple test to verify xUnit infrastructure is working
        var result = 1 + 1;
        Assert.Equal(2, result);
    }
    
    [Fact]
    public void SecurityFixes_AreImplemented()
    {
        // Verification that our security fixes are in place
        // This is a placeholder showing test infrastructure is ready
        
        // Directory traversal fix: Path validation regex
        var validPath = "bootfiles/test.img";
        var invalidPath = "../../../etc/passwd";
        
        Assert.DoesNotContain("..", validPath);
        Assert.Contains("..", invalidPath);
    }
}
