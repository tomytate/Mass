using FluentValidation;
using Mass.Spec.Contracts.Agent;

namespace Mass.Core.Validation;

public class AgentRegistrationRequestValidator : AbstractValidator<AgentRegistrationRequest>
{
    public AgentRegistrationRequestValidator()
    {
        RuleFor(x => x.Hostname)
            .NotEmpty().WithMessage("Hostname is required")
            .MaximumLength(100).WithMessage("Hostname must not exceed 100 characters");

        RuleFor(x => x.MacAddress)
            .NotEmpty().WithMessage("MAC Address is required")
            .Matches(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$")
            .WithMessage("Invalid MAC Address format (e.g., 00:11:22:33:44:55)");

        RuleFor(x => x.OsVersion)
            .NotEmpty().WithMessage("OS Version is required");

        RuleFor(x => x.AgentVersion)
            .NotEmpty().WithMessage("Agent Version is required");
    }
}

public class AgentHeartbeatRequestValidator : AbstractValidator<AgentHeartbeatRequest>
{
    public AgentHeartbeatRequestValidator()
    {
        RuleFor(x => x.AgentId)
            .NotEmpty().WithMessage("Agent ID is required");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(status => status is "Idle" or "Busy" or "Error" or "Offline")
            .WithMessage("Status must be one of: Idle, Busy, Error, Offline");

        RuleFor(x => x.CpuUsage)
            .InclusiveBetween(0, 100).WithMessage("CPU Usage must be between 0 and 100");

        RuleFor(x => x.MemoryUsage)
            .GreaterThanOrEqualTo(0).WithMessage("Memory Usage must be non-negative");
    }
}
