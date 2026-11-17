using FluentValidation;
using SpaceHub.Application.Catalog;
namespace SpaceHub.Application.Validators;

public sealed class UpdateServiceRequestValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceRequestValidator()
    {
        RuleFor(x => new CreateServiceRequest(x.Name, x.PricingType, x.UnitPrice, x.SpaceRowVersion))
            .SetValidator(new CreateServiceRequestValidator()).OverridePropertyName("");
        RuleFor(x => x.IsActive).NotNull();
    }
}
