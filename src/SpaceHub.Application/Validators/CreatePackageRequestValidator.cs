using FluentValidation;
using SpaceHub.Application.Catalog;
namespace SpaceHub.Application.Validators;

public sealed class CreatePackageRequestValidator : AbstractValidator<CreatePackageRequest>
{
    public CreatePackageRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120).Must(x => x is not null && x.Trim().Length >= 2);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(3000);
        RuleFor(x => x.Price).InclusiveBetween(0m, 1000000m).PrecisionScale(18, 2, true);
        RuleFor(x => x.SpaceRowVersion).NotNull().Must(x => x is { Length: 8 }).WithMessage("Use the latest spaceRowVersion returned by the API.");
        RuleFor(x => x.Items).NotEmpty().Must(x => x is null || x.Count <= 50).WithMessage("At most 50 services are allowed.")
            .Must(x => x is null || x.All(i => i is not null) && x.Select(i => i.SpaceServiceId).Distinct().Count() == x.Count)
            .WithMessage("Services must be non-null and unique.");
        RuleForEach(x => x.Items).SetValidator(new PackageItemRequestValidator());
    }
}
