namespace EMS.Web.Validators;

using FluentValidation;
using EMS.Core.Interfaces;

public class DashboardFilterValidator : AbstractValidator<DashboardFilterDto>
{
    public DashboardFilterValidator()
    {
        RuleFor(x => x.Plant)
            .NotEmpty().WithMessage("Plant filter is required")
            .MaximumLength(100).WithMessage("Plant filter cannot exceed 100 characters");

        RuleFor(x => x.Building)
            .NotEmpty().WithMessage("Building filter is required")
            .MaximumLength(100).WithMessage("Building filter cannot exceed 100 characters");

        RuleFor(x => x.Area)
            .NotEmpty().WithMessage("Area filter is required")
            .MaximumLength(100).WithMessage("Area filter cannot exceed 100 characters");

        RuleFor(x => x.DateFrom)
            .NotEmpty().WithMessage("DateFrom is required")
            .LessThanOrEqualTo(x => x.DateTo).WithMessage("DateFrom must be before DateTo");

        RuleFor(x => x.DateTo)
            .NotEmpty().WithMessage("DateTo is required")
            .GreaterThanOrEqualTo(x => x.DateFrom).WithMessage("DateTo must be after DateFrom");
    }
}
