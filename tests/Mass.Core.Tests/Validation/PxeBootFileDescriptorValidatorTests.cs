using FluentValidation.TestHelper;
using Mass.Core.Validation;
using Mass.Spec.Contracts.Pxe;
using Xunit;

namespace Mass.Core.Tests.Validation;

public class PxeBootFileDescriptorValidatorTests
{
    private readonly PxeBootFileDescriptorValidator _validator;

    public PxeBootFileDescriptorValidatorTests()
    {
        _validator = new PxeBootFileDescriptorValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var descriptor = new PxeBootFileDescriptor { Id = "" };
        var result = _validator.TestValidate(descriptor);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Should_Have_Error_When_FileName_Is_Empty()
    {
        var descriptor = new PxeBootFileDescriptor { FileName = "" };
        var result = _validator.TestValidate(descriptor);
        result.ShouldHaveValidationErrorFor(x => x.FileName)
              .WithErrorMessage("Filename is required");
    }

    [Fact]
    public void Should_Have_Error_When_Size_Is_Zero_Or_Negative()
    {
        var descriptor = new PxeBootFileDescriptor { Size = 0 };
        var result = _validator.TestValidate(descriptor);
        result.ShouldHaveValidationErrorFor(x => x.Size)
              .WithErrorMessage("File size must be positive");

        descriptor.Size = -1;
        result = _validator.TestValidate(descriptor);
        result.ShouldHaveValidationErrorFor(x => x.Size)
              .WithErrorMessage("File size must be positive");
    }

    [Fact]
    public void Should_Have_Error_When_Architecture_Is_Empty()
    {
        var descriptor = new PxeBootFileDescriptor { Architecture = "" };
        var result = _validator.TestValidate(descriptor);
        result.ShouldHaveValidationErrorFor(x => x.Architecture);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Descriptor_Is_Valid()
    {
        var descriptor = new PxeBootFileDescriptor
        {
            Id = "bootx64.efi",
            FileName = "bootx64.efi",
            Size = 1024,
            Architecture = "x64"
        };
        var result = _validator.TestValidate(descriptor);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
