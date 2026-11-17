using FluentValidation;
using SpaceHub.Application.Events;
namespace SpaceHub.Application.Validators;

public sealed class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        RuleFor(x => new CreateEventRequest(1, x.Title, x.Description, x.StartsAt, x.EndsAt, x.MaxAttendees, x.TicketPrice, x.Visibility, x.TagIds))
            .SetValidator(new CreateEventRequestValidator()).OverridePropertyName("");
        RuleFor(x => new EventVersionRequest(x.RowVersion)).SetValidator(new EventVersionRequestValidator()).OverridePropertyName("");
    }
}
