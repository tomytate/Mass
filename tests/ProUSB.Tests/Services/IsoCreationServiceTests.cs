using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProUSB.Domain;
using ProUSB.Services.IsoCreation;
using Xunit;

namespace ProUSB.Tests.Services;

public class IsoCreationServiceTests
{
    private readonly Mock<ILogger<IsoCreationService>> _loggerMock;
    private readonly IsoCreationService _service;

    public IsoCreationServiceTests()
    {
        _loggerMock = new Mock<ILogger<IsoCreationService>>();
        _service = new IsoCreationService(_loggerMock.Object);
    }

    [Fact]
    public async Task CreateIsoFromDeviceAsync_WithInvalidMode_ShouldThrowException()
    {
        // Arrange
        var device = new UsbDeviceInfo { DeviceId = "dev1", TotalSize = 1000 };
        var outputPath = "output.iso";
        var invalidMode = (IsoCreationMode)999;

        // Act
        Func<Task> act = async () => await _service.CreateIsoFromDeviceAsync(
            device, outputPath, invalidMode, null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Unsupported creation mode: {invalidMode}");
    }

    [Fact]
    public async Task CreateIsoFromDeviceAsync_OnNonWindows_ShouldReturnFailure()
    {
        if (OperatingSystem.IsWindows())
        {
            return; // Skip on Windows as the method should proceed (mocking P/Invoke is hard in unit test)
        }

        // Arrange
        var device = new UsbDeviceInfo { DeviceId = "dev1", PhysicalIndex = 1, TotalSize = 1000 };

        // Act
        var result = await _service.CreateIsoFromDeviceAsync(
            device, "out.iso", IsoCreationMode.RawCopy, null!, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("only supported on Windows");
    }
}
