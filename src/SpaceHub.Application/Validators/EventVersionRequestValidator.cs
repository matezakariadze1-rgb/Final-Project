using FluentValidation;
using SpaceHub.Application.Events;
namespace SpaceHub.Application.Validators;

public sealed class EventVersionRequestValidator : AbstractValidator<EventVersionRequest>
{
    public EventVersionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotNull().Must(x => x is { Length: 8 }).WithMessage("Use the latest event rowVersion.");
    }
}
