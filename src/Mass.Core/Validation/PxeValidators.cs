using FluentValidation;
using Mass.Spec.Contracts.Pxe;

namespace Mass.Core.Validation;

public class PxeBootFileDescriptorValidator : AbstractValidator<PxeBootFileDescriptor>
{
    public PxeBootFileDescriptorValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().WithMessage("Filename is required");
        RuleFor(x => x.Size).GreaterThan(0).WithMessage("File size must be positive");
        RuleFor(x => x.Architecture).NotEmpty();
    }
}
