using FluentValidation;
using SpaceHub.Application.Events;
namespace SpaceHub.Application.Validators;

public sealed class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.BookingId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120).Must(x => x is not null && x.Trim().Length >= 3);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(3000).Must(x => x is not null && x.Trim().Length >= 10);
        RuleFor(x => x.StartsAt).Must(x => x.UtcDateTime >= DateTime.UtcNow.AddHours(1)).WithMessage("Start must be at least one hour in the future.");
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt).Must((r, e) => e - r.StartsAt <= TimeSpan.FromDays(7)).WithMessage("Duration cannot exceed seven days.");
        RuleFor(x => x.MaxAttendees).InclusiveBetween(1, 100000);
        RuleFor(x => x.TicketPrice).InclusiveBetween(0m, 10000m).PrecisionScale(18, 2, true);
        RuleFor(x => x.Visibility).IsInEnum();
        RuleFor(x => x.TagIds).NotNull().Must(x => x is null || x.Count <= 5).WithMessage("At most 5 tags are allowed.")
            .Must(x => x is null || x.Distinct().Count() == x.Count).WithMessage("Tags must be unique.");
        RuleForEach(x => x.TagIds).GreaterThan(0);
    }
}
