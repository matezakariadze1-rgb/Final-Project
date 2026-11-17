using FluentValidation;
using SpaceHub.Application.Catalog;
namespace SpaceHub.Application.Validators;

public sealed class PackageItemRequestValidator : AbstractValidator<PackageItemRequest>
{
    public PackageItemRequestValidator()
    {
        RuleFor(x => x.SpaceServiceId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 100000);
    }
}
