using FluentValidation.TestHelper;
using Mass.Core.Validation;
using Mass.Spec.Contracts.Usb;
using Xunit;

namespace Mass.Core.Tests.Validation;

public class UsbJobValidatorTests
{
    private readonly UsbJobValidator _validator;

    public UsbJobValidatorTests()
    {
        _validator = new UsbJobValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var job = new UsbJob { Id = "" };
        var result = _validator.TestValidate(job);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Should_Have_Error_When_ImagePath_Is_Empty()
    {
        var job = new UsbJob { ImagePath = "" };
        var result = _validator.TestValidate(job);
        result.ShouldHaveValidationErrorFor(x => x.ImagePath);
    }

    [Theory]
    [InlineData("image.txt")]
    [InlineData("image")]
    [InlineData("image.exe")]
    public void Should_Have_Error_When_ImagePath_Has_Invalid_Extension(string path)
    {
        var job = new UsbJob { ImagePath = path };
        var result = _validator.TestValidate(job);
        result.ShouldHaveValidationErrorFor(x => x.ImagePath)
              .WithErrorMessage("Image file must be an .iso or .img file");
    }

    [Theory]
    [InlineData("image.iso")]
    [InlineData("image.img")]
    [InlineData("IMAGE.ISO")]
    public void Should_Not_Have_Error_When_ImagePath_Is_Valid(string path)
    {
        var job = new UsbJob { ImagePath = path };
        var result = _validator.TestValidate(job);
        result.ShouldNotHaveValidationErrorFor(x => x.ImagePath);
    }

    [Fact]
    public void Should_Have_Error_When_TargetDeviceId_Is_Empty()
    {
        var job = new UsbJob { TargetDeviceId = "" };
        var result = _validator.TestValidate(job);
        result.ShouldHaveValidationErrorFor(x => x.TargetDeviceId);
    }

    [Fact]
    public void Should_Have_Error_When_VolumeLabel_Is_Too_Long()
    {
        var job = new UsbJob { VolumeLabel = new string('a', 33) };
        var result = _validator.TestValidate(job);
        result.ShouldHaveValidationErrorFor(x => x.VolumeLabel);
    }

    [Fact]
    public void Should_Have_Error_When_VolumeLabel_Contains_Invalid_Characters()
    {
        var job = new UsbJob { VolumeLabel = "Invalid Label!" };
        var result = _validator.TestValidate(job);
        result.ShouldHaveValidationErrorFor(x => x.VolumeLabel);
    }

    [Theory]
    [InlineData("GPT")]
    [InlineData("MBR")]
    public void Should_Not_Have_Error_When_PartitionScheme_Is_Valid(string scheme)
    {
        var job = new UsbJob { PartitionScheme = scheme };
        var result = _validator.TestValidate(job);
        result.ShouldNotHaveValidationErrorFor(x => x.PartitionScheme);
    }

    [Fact]
    public void Should_Have_Error_When_PartitionScheme_Is_Invalid()
    {
        var job = new UsbJob { PartitionScheme = "Invalid" };
        var result = _validator.TestValidate(job);
        result.ShouldHaveValidationErrorFor(x => x.PartitionScheme);
    }

    [Theory]
    [InlineData("FAT32")]
    [InlineData("NTFS")]
    [InlineData("ExFAT")]
    public void Should_Not_Have_Error_When_FileSystem_Is_Valid(string fs)
    {
        var job = new UsbJob { FileSystem = fs };
        var result = _validator.TestValidate(job);
        result.ShouldNotHaveValidationErrorFor(x => x.FileSystem);
    }

    [Fact]
    public void Should_Have_Error_When_FileSystem_Is_Invalid()
    {
        var job = new UsbJob { FileSystem = "Invalid" };
        var result = _validator.TestValidate(job);
        result.ShouldHaveValidationErrorFor(x => x.FileSystem);
    }
}
