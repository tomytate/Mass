using FluentValidation;
using Mass.Spec.Contracts.Usb;

namespace Mass.Core.Validation;

public class UsbJobValidator : AbstractValidator<UsbJob>
{
    public UsbJobValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        
        RuleFor(x => x.ImagePath)
            .NotEmpty().WithMessage("Image path is required")
            .Must(path => path.EndsWith(".iso", StringComparison.OrdinalIgnoreCase) || 
                          path.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Image file must be an .iso or .img file");

        RuleFor(x => x.TargetDeviceId).NotEmpty().WithMessage("Target device is required");

        RuleFor(x => x.VolumeLabel)
            .MaximumLength(32).WithMessage("Volume label too long") // FAT32 limit is 11, NTFS is 32. Safe bet 32.
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Volume label contains invalid characters");

        RuleFor(x => x.PartitionScheme)
            .Must(x => x == "GPT" || x == "MBR")
            .WithMessage("Partition scheme must be GPT or MBR");

        RuleFor(x => x.FileSystem)
            .Must(x => x == "FAT32" || x == "NTFS" || x == "ExFAT")
            .WithMessage("File system not supported");
    }
}
