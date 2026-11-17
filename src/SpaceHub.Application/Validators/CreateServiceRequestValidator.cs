using FluentValidation;
using SpaceHub.Application.Catalog;
namespace SpaceHub.Application.Validators;

public sealed class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120).Must(x => x is not null && x.Trim().Length >= 2);
        RuleFor(x => x.PricingType).IsInEnum();
        RuleFor(x => x.UnitPrice).InclusiveBetween(0m, 100000m).PrecisionScale(18, 2, true);
        RuleFor(x => x.SpaceRowVersion).NotNull().Must(x => x is { Length: 8 }).WithMessage("Use the latest spaceRowVersion returned by the API.");
    }
}
