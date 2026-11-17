using FluentValidation;
using SpaceHub.Application.Bookings;
namespace SpaceHub.Application.Validators;

public sealed class ConfirmBookingRequestValidator : AbstractValidator<ConfirmBookingRequest>
{
    public ConfirmBookingRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotNull().Must(x => x is { Length: 8 }).WithMessage("Use the current booking rowVersion.");
    }
}
