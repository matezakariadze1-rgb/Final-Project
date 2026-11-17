using FluentValidation;
using SpaceHub.Application.Spaces;
namespace SpaceHub.Application.Validators;

public sealed class UpdateSpaceStatusRequestValidator : AbstractValidator<UpdateSpaceStatusRequest>
{
    public UpdateSpaceStatusRequestValidator()
    {
        RuleFor(x => x.IsActive).NotNull();
        RuleFor(x => x.RowVersion).NotNull().Must(x => x is { Length: 8 }).WithMessage("A valid RowVersion is required.");
    }
}
