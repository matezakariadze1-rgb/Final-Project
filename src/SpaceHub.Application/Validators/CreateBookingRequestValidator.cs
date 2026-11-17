using FluentValidation;
using SpaceHub.Application.Bookings;
namespace SpaceHub.Application.Validators;

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => new BookingQuoteRequest(x.SpaceId, x.StartsAt, x.EndsAt, x.AttendeeCount, x.PackageId, x.AdditionalServices))
            .SetValidator(new BookingQuoteRequestValidator()).OverridePropertyName("");
        RuleFor(x => x.SpaceRowVersion).NotNull().Must(x => x is { Length: 8 }).WithMessage("Use the spaceRowVersion returned by a fresh quote.");
    }
}
