using FluentValidation;
using SpaceHub.Application.Auth;
namespace SpaceHub.Application.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120)
            .Must(x => x is not null && x.Trim().Length >= 2).WithMessage("Full name must contain at least 2 characters.");
    }
}
