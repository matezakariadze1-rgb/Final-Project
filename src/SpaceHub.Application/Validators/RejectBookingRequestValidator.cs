using FluentValidation;
using SpaceHub.Application.Bookings;
namespace SpaceHub.Application.Validators;

public sealed class RejectBookingRequestValidator : AbstractValidator<RejectBookingRequest>
{
    public RejectBookingRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => new ConfirmBookingRequest(x.RowVersion)).SetValidator(new ConfirmBookingRequestValidator()).OverridePropertyName("");
    }
}
