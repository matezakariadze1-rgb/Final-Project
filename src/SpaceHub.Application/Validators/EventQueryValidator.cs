using FluentValidation;
using SpaceHub.Application.Events;
namespace SpaceHub.Application.Validators;

public sealed class EventQueryValidator : AbstractValidator<EventQuery>
{
    public EventQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(120);
        RuleFor(x => x.City).MaximumLength(60);
        RuleFor(x => x.TagId).GreaterThan(0).When(x => x.TagId.HasValue);
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).When(x => x.From.HasValue && x.To.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Page).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
