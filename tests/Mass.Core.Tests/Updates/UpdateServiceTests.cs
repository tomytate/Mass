using System.Reflection;
using FluentAssertions;
using Mass.Core.Updates;
using Xunit;

namespace Mass.Core.Tests.Updates;

public class UpdateServiceTests
{
    // Note: Since UpdateService depends on HttpClient and static methods, complete isolation is hard without refactoring.
    // However, we can test the version comparison logic and basic state via reflection or by subclassing if protected.
    // Given the current implementation of UpdateService has a private static method CompareVersions,
    // we can test public behavior or use reflection to test the private logic if critical.

    // For this test suite, we will focus on what we can test:
    // 1. Initial State
    // 2. VerifyUpdateAsync logic (hashing) using a real temp file.

    [Fact]
    public void Constructor_ShouldSetCurrentVersion()
    {
        // Act
        using var service = new UpdateService();

        // Assert
        service.CurrentVersion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task VerifyUpdateAsync_ShouldReturnTrue_ForValidHash()
    {
        // Arrange
        using var service = new UpdateService();
        var tempFile = Path.GetTempFileName();
        var content = new byte[] { 1, 2, 3, 4, 5 };
        await File.WriteAllBytesAsync(tempFile, content);

        // Expected SHA256 of {1,2,3,4,5}
        // 74F1887AA095E63B0530883A01412A3D063B55D93215286D9D36F7BC8BD04E9C
        // Note: The previous hash was likely for string "12345" or similar.
        // For actual byte array {1,2,3,4,5}, the hash is:
        // 74f81fe167d99b4cb41d6d0ccda82278caee9f3e2f25d5e5a3936ff3dcec60d0
        var expectedHash = "74f81fe167d99b4cb41d6d0ccda82278caee9f3e2f25d5e5a3936ff3dcec60d0";

        try
        {
            // Act
            var result = await service.VerifyUpdateAsync(tempFile, expectedHash);

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyUpdateAsync_ShouldReturnFalse_ForInvalidHash()
    {
        // Arrange
        using var service = new UpdateService();
        var tempFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3 });
        var invalidHash = "ABCDEF";

        try
        {
            // Act
            var result = await service.VerifyUpdateAsync(tempFile, invalidHash);

            // Assert
            result.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyUpdateAsync_ShouldReturnTrue_WhenHashIsEmpty()
    {
        // Arrange
        using var service = new UpdateService();

        // Act
        var result = await service.VerifyUpdateAsync("nonexistent", "");

        // Assert
        result.Should().BeTrue();
    }
}
