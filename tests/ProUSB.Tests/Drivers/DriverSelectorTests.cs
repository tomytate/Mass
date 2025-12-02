using FluentAssertions;
using Moq;
using ProUSB.Drivers;
using Xunit;

namespace ProUSB.Tests.Drivers;

public class DriverSelectorTests
{
    [Fact]
    public void SelectBestDriver_WithNativeDriver_ReturnsNativeDriver()
    {
        // Arrange
        var nativeDriver = new Mock<IDriverAdapter>();
        nativeDriver.Setup(d => d.Name).Returns("Native Win32 API");
        nativeDriver.Setup(d => d.IsAvailable).Returns(true);

        var otherDriver = new Mock<IDriverAdapter>();
        otherDriver.Setup(d => d.Name).Returns("Other Driver");
        otherDriver.Setup(d => d.IsAvailable).Returns(true);

        var selector = new DriverSelector(new[] { otherDriver.Object, nativeDriver.Object });

        // Act
        var selected = selector.SelectBestDriver();

        // Assert
        selected.Should().Be(nativeDriver.Object);
        selected.Name.Should().Contain("Native");
    }

    [Fact]
    public void SelectBestDriver_WhenNoDriversAvailable_ThrowsException()
    {
        // Arrange
        var unavailableDriver = new Mock<IDriverAdapter>();
        unavailableDriver.Setup(d => d.IsAvailable).Returns(false);

        var selector = new DriverSelector(new[] { unavailableDriver.Object });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => selector.SelectBestDriver());
    }

    [Fact]
    public void GetAvailableDrivers_ReturnsOnlyAvailableDrivers()
    {
        // Arrange
        var available1 = new Mock<IDriverAdapter>();
        available1.Setup(d => d.IsAvailable).Returns(true);

        var unavailable = new Mock<IDriverAdapter>();
        unavailable.Setup(d => d.IsAvailable).Returns(false);

        var available2 = new Mock<IDriverAdapter>();
        available2.Setup(d => d.IsAvailable).Returns(true);

        var selector = new DriverSelector(new[] { available1.Object, unavailable.Object, available2.Object });

        // Act
        var drivers = selector.GetAvailableDrivers().ToList();

        // Assert
        drivers.Should().HaveCount(2);
        drivers.Should().Contain(available1.Object);
        drivers.Should().Contain(available2.Object);
        drivers.Should().NotContain(unavailable.Object);
    }
}
