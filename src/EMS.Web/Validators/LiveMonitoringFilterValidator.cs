namespace EMS.Web.Validators;

using FluentValidation;
using EMS.Core.Interfaces;

public class LiveMonitoringFilterValidator : AbstractValidator<LiveMonitoringFilterDto>
{
    private static readonly string[] ValidStatuses = { "all", "online", "offline", "warning", "unknown" };

    public LiveMonitoringFilterValidator()
    {
        RuleFor(x => x.Plant)
            .NotEmpty().WithMessage("Plant filter is required")
            .MaximumLength(100).WithMessage("Plant filter cannot exceed 100 characters");

        RuleFor(x => x.Building)
            .NotEmpty().WithMessage("Building filter is required")
            .MaximumLength(100).WithMessage("Building filter cannot exceed 100 characters");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status filter is required")
            .Must(x => ValidStatuses.Contains(x.ToLower())).WithMessage("Status must be one of: all, online, offline, warning, unknown");

        RuleFor(x => x.IncludeSparklines)
            .NotNull().WithMessage("IncludeSparklines flag is required");
    }
}
