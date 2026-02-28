using FluentAssertions;
using Mass.Core.Interfaces;
using Mass.Spec.Contracts.Pxe;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProPXEServer.API.Data;
using ProPXEServer.API.Services;
using Xunit;

namespace Mass.Core.Tests.Services;

public class PxeManagerTests : IDisposable
{
    private readonly PxeManager _service;
    private readonly Mock<ILogger<PxeManager>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly string _testRoot;
    private readonly string _pxeRoot;

    public PxeManagerTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "MassPxeTest_" + Guid.NewGuid());
        _pxeRoot = Path.Combine(_testRoot, "wwwroot", "pxe");
        Directory.CreateDirectory(_pxeRoot);

        _loggerMock = new Mock<ILogger<PxeManager>>();
        _configMock = new Mock<IConfiguration>();

        // Passing null for ApplicationDbContext as it is unused in file methods
        _service = new PxeManager(null!, _loggerMock.Object, _configMock.Object, _pxeRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testRoot)) Directory.Delete(_testRoot, true);
        }
        catch {}
    }

    [Fact]
    public async Task UploadBootFileAsync_ShouldCreateFile()
    {
        // Arrange
        var descriptor = new PxeBootFileDescriptor
        {
            FileName = "test.efi",
            Architecture = "x64_UEFI",
            Size = 100
        };

        // Act
        var result = await _service.UploadBootFileAsync(descriptor);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var expectedPath = Path.Combine(_pxeRoot, "x64_uefi", "test.efi");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task UploadBootFileAsync_ShouldPreventPathTraversal()
    {
        // Arrange
        var descriptor = new PxeBootFileDescriptor
        {
            // Note: Path.Combine("root", "arch", "../outside.efi") normalizes to "root/outside.efi"
            // To trigger traversal failure, we need to go up enough levels to escape _pxeRoot.
            // If _pxeRoot is /tmp/test/wwwroot/pxe, we need to go up at least 4 levels.
            // However, Path.Combine handles .. navigation. The issue is likely that "outside.efi" IS within the root folder
            // after combination if architecture folder is used.
            // Architecture is "x64_uefi" (from default lowercasing).
            // Target dir: _pxeRoot/x64_uefi
            // File path: _pxeRoot/x64_uefi/../outside.efi -> _pxeRoot/outside.efi
            // This IS within _pxeRoot, so it passes the check!
            // We need to attempt to write OUTSIDE _pxeRoot.
            FileName = "../../outside.efi",
            Architecture = "x64_UEFI",
            Size = 100
        };

        // Act
        var result = await _service.UploadBootFileAsync(descriptor);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid file path");
    }

    [Fact]
    public async Task ListBootFilesAsync_ShouldReturnFiles()
    {
        // Arrange
        var archDir = Path.Combine(_pxeRoot, "x64_uefi");
        Directory.CreateDirectory(archDir);
        await File.WriteAllTextAsync(Path.Combine(archDir, "boot.efi"), "data");

        // Act
        var files = await _service.ListBootFilesAsync();

        // Assert
        files.Should().Contain(f => f.FileName == "boot.efi" && f.Architecture == "x64_UEFI");
    }

    [Fact]
    public async Task DeleteBootFileAsync_ShouldDeleteFile()
    {
        // Arrange
        var relativePath = "test_delete.efi";
        var fullPath = Path.Combine(_pxeRoot, relativePath);
        await File.WriteAllTextAsync(fullPath, "data");

        // Act
        var result = await _service.DeleteBootFileAsync(relativePath);

        // Assert
        result.Should().BeTrue();
        File.Exists(fullPath).Should().BeFalse();
    }
}
