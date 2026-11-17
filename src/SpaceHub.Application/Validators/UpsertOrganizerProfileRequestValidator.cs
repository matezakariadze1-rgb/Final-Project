using FluentValidation;
using SpaceHub.Application.Events;
namespace SpaceHub.Application.Validators;

public sealed class UpsertOrganizerProfileRequestValidator : AbstractValidator<UpsertOrganizerProfileRequest>
{
    public UpsertOrganizerProfileRequestValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(100).Must(x => x is not null && x.Trim().Length >= 2);
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^(\+995)?5\d{8}$");
        RuleFor(x => x.Website).MaximumLength(2048).Must(x => string.IsNullOrWhiteSpace(x)
            || Uri.TryCreate(x, UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http") && !string.IsNullOrWhiteSpace(uri.Host))
            .WithMessage("Website must be an HTTP(S) URL or empty.");
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
