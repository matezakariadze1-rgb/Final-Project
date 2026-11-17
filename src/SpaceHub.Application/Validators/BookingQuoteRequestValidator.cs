using FluentValidation;
using SpaceHub.Application.Bookings;
namespace SpaceHub.Application.Validators;

public sealed class BookingQuoteRequestValidator : AbstractValidator<BookingQuoteRequest>
{
    public BookingQuoteRequestValidator()
    {
        RuleFor(x => x.SpaceId).GreaterThan(0);
        RuleFor(x => x.StartsAt).Must(x => x.UtcDateTime > DateTime.UtcNow).WithMessage("Start must be in the future.");
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt);
        RuleFor(x => x).Must(x => x.EndsAt - x.StartsAt >= TimeSpan.FromHours(1) && x.EndsAt - x.StartsAt <= TimeSpan.FromHours(24))
            .WithMessage("Duration must be between 1 and 24 hours.");
        RuleFor(x => x.StartsAt).Must(x => x.UtcTicks % TimeSpan.FromMinutes(30).Ticks == 0).WithMessage("Use 30-minute intervals.");
        RuleFor(x => x.EndsAt).Must(x => x.UtcTicks % TimeSpan.FromMinutes(30).Ticks == 0).WithMessage("Use 30-minute intervals.");
        RuleFor(x => x.AttendeeCount).InclusiveBetween(1, 100000);
        RuleFor(x => x.PackageId).GreaterThan(0).When(x => x.PackageId.HasValue);
        RuleFor(x => x.AdditionalServices).NotNull().Must(x => x is null || x.Count <= 50).WithMessage("At most 50 additional services are allowed.")
            .Must(x => x is null || x.All(i => i is not null) && x.Select(i => i.SpaceServiceId).Distinct().Count() == x.Count)
            .WithMessage("Additional services must be non-null and unique.");
        RuleForEach(x => x.AdditionalServices).SetValidator(new RequestedServiceValidator());
    }
}
