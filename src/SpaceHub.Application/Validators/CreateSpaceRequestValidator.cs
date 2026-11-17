using FluentValidation;
using SpaceHub.Application.Spaces;
namespace SpaceHub.Application.Validators;

public sealed class CreateSpaceRequestValidator : AbstractValidator<CreateSpaceRequest>
{
    public CreateSpaceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120).Must(x => x is not null && x.Trim().Length >= 2);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(3000);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(200).Must(x => x is not null && x.Trim().Length >= 5);
        RuleFor(x => x.City).NotEmpty().MaximumLength(60).Must(x => x is not null && x.Trim().Length >= 2);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 100000);
        RuleFor(x => x.HourlyRate).InclusiveBetween(0m, 100000m).PrecisionScale(18, 2, true);
    }
}
