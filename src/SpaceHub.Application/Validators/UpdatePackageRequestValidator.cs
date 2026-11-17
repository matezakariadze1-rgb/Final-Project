using FluentValidation;
using SpaceHub.Application.Catalog;
namespace SpaceHub.Application.Validators;

public sealed class UpdatePackageRequestValidator : AbstractValidator<UpdatePackageRequest>
{
    public UpdatePackageRequestValidator()
    {
        RuleFor(x => new CreatePackageRequest(x.Name, x.Description, x.Price, x.Items, x.SpaceRowVersion))
            .SetValidator(new CreatePackageRequestValidator()).OverridePropertyName("");
        RuleFor(x => x.IsActive).NotNull();
    }
}
