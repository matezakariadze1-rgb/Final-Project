using FluentValidation;
using SpaceHub.Application.Registrations;
namespace SpaceHub.Application.Validators;

public sealed class AttendeeQueryValidator : AbstractValidator<AttendeeQuery>
{
    public AttendeeQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Search).MaximumLength(120);
        RuleFor(x => x.Page).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
