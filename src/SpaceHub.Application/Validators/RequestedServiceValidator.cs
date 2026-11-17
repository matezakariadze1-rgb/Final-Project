using FluentValidation;
using SpaceHub.Application.Bookings;
namespace SpaceHub.Application.Validators;

public sealed class RequestedServiceValidator : AbstractValidator<RequestedService>
{
    public RequestedServiceValidator()
    {
        RuleFor(x => x.SpaceServiceId).GreaterThan(0);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 100000).When(x => x.Quantity.HasValue);
    }
}
