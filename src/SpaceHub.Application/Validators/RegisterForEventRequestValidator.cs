using FluentValidation;
using SpaceHub.Application.Registrations;
namespace SpaceHub.Application.Validators;

public sealed class RegisterForEventRequestValidator : AbstractValidator<RegisterForEventRequest>
{
    public RegisterForEventRequestValidator()
    {
        RuleFor(x => x.AccessCode).MaximumLength(64);
    }
}
