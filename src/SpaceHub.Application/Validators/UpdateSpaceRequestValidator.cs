using FluentValidation;
using SpaceHub.Application.Spaces;
namespace SpaceHub.Application.Validators;

public sealed class UpdateSpaceRequestValidator : AbstractValidator<UpdateSpaceRequest>
{
    public UpdateSpaceRequestValidator()
    {
        RuleFor(x => new CreateSpaceRequest(x.Name, x.Description, x.Address, x.City, x.Capacity, x.HourlyRate))
            .SetValidator(new CreateSpaceRequestValidator()).OverridePropertyName("");
        RuleFor(x => x.RowVersion).NotNull().Must(x => x is { Length: 8 }).WithMessage("RowVersion must be the 8-byte version returned by the API.");
    }
}
