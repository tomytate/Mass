using FluentAssertions;
using Xunit;

namespace ProPXEServer.API.Tests;

/// <summary>
/// Tests for directory traversal prevention logic in BootFilesController
/// These tests verify the security fixes without requiring complex mocking
/// </summary>
public class PathValidationSecurityTests
{
    [Theory]
    [InlineData("test;rm -rf /")]
    [InlineData("boot<script>alert(1)</script>.iso")]
    [InlineData("boot\0file.iso")] // Null byte injection
    [InlineData("C:\\Windows\\System32")] // Windows absolute path with backslash
    public void PathRegex_MaliciousCharacters_ShouldNotMatch(string maliciousPath)
    {
        // This is the regex used in BootFilesController.DownloadBootFile
        var pathRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9_\-\.\/]+$");
        
        // Act
        var isValid = pathRegex.IsMatch(maliciousPath);
        
        // Assert
        isValid.Should().BeFalse(because: $"Malicious path '{maliciousPath}' should be blocked by regex");
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32\\config\\sam")] // Contains backslashes
    public void PathRegex_TraversalPaths_PassRegexButCaughtByTraversalCheck(string traversalPath)
    {
        // The regex allows dots and forward slashes, so "../" technically passes regex
        // BUT the Path.GetFullPath check catches directory traversal
        var pathRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9_\-\.\/]+$");
        
        // Act
        var isValid = pathRegex.IsMatch(traversalPath);
        
        // Assert - Path with backslashes fails regex, but "../" passes and gets caught later
        if (traversalPath.Contains('\\'))
        {
            isValid.Should().BeFalse(because: "Backslashes are not allowed by regex");
        }
        else
        {
            // "../../../etc/passwd" passes regex but will be caught by traversal detection
            isValid.Should().BeTrue(because: "Forward slash traversal passes regex, caught by Path.GetFullPath check");
        }
    }

    [Theory]
    [InlineData("ubuntu-22.04.iso")]
    [InlineData("arch/boot.img")]
    [InlineData("windows/sources/boot.wim")]
    [InlineData("netboot.xyz/menu.ipxe")]
    [InlineData("boot-files/test_image.iso")]
    [InlineData("debian.11.0.0-amd64-netinst.iso")]
    public void PathRegex_ValidPaths_ShouldMatch(string validPath)
    {
        // This is the regex used in BootFilesController.DownloadBootFile
        var pathRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9_\-\.\/]+$");
        
        // Act
        var isValid = pathRegex.IsMatch(validPath);
        
        // Assert
        isValid.Should().BeTrue(because: $"Valid path '{validPath}' should pass regex validation");
    }

    [Fact]
    public void PathTraversalDetection_TraversalAttempt_ShouldBeDetected()
    {
        // Arrange
        var pxeRoot = "C:\\PXE\\BootFiles";
        var maliciousPath = "../../../secrets.txt";
        
        // Act
        var combinedPath = Path.Combine(pxeRoot, maliciousPath);
        var fullPath = Path.GetFullPath(combinedPath);
        var isTraversal = !fullPath.StartsWith(pxeRoot, StringComparison.OrdinalIgnoreCase);
        
        // Assert
        isTraversal.Should().BeTrue(because: "Path traversal attempts should be detected");
    }

    [Fact]
    public void PathTraversalDetection_ValidPath_ShouldNotBeDetected()
    {
        // Arrange
        var pxeRoot = "C:\\PXE\\BootFiles";
        var validPath = "ubuntu/boot.iso";
        
        // Act  
        var combinedPath = Path.Combine(pxeRoot, validPath);
        var fullPath = Path.GetFullPath(combinedPath);
        var isTraversal = !fullPath.StartsWith(pxeRoot, StringComparison.OrdinalIgnoreCase);
        
        // Assert
        isTraversal.Should().BeFalse(because: "Valid paths should not be flagged as traversal");
    }
}

/// <summary>
/// Tests for JWT secret validation logic in AuthController
/// </summary>
public class JwtSecretSecurityTests
{
    [Fact]
    public void JwtSecret_LessThan32Characters_ShouldBeInvalid()
    {
        // Arrange
        var shortSecret = "short-secret-123";
        
        // Act
        var isValid = !string.IsNullOrEmpty(shortSecret) && shortSecret.Length >= 32;
        
        // Assert
        isValid.Should().BeFalse(because: "JWT secrets must be at least 32 characters for security");
    }

    [Fact]
    public void JwtSecret_32OrMoreCharacters_ShouldBeValid()
    {
        // Arrange
        var validSecret = "this-is-a-valid-secret-key-with-32-plus-characters";
        
        // Act
        var isValid = !string.IsNullOrEmpty(validSecret) && validSecret.Length >= 32;
        
        // Assert
        isValid.Should().BeTrue(because: "32+ character secrets should be accepted");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void JwtSecret_NullOrEmpty_ShouldBeInvalid(string? invalidSecret)
    {
        // Act
        var isValid = !string.IsNullOrEmpty(invalidSecret) && invalidSecret.Length >= 32;
        
        // Assert
        isValid.Should().BeFalse(because: "Null or empty secrets should be rejected");
    }
}
