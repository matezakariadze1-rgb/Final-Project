using FluentValidation;
using SpaceHub.Application.Bookings;
namespace SpaceHub.Application.Validators;

public sealed class CancelBookingRequestValidator : AbstractValidator<CancelBookingRequest>
{
    public CancelBookingRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(1000);
        RuleFor(x => new ConfirmBookingRequest(x.RowVersion)).SetValidator(new ConfirmBookingRequestValidator()).OverridePropertyName("");
    }
}
