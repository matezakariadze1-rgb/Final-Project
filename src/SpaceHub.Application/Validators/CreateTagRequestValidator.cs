using FluentValidation;
using SpaceHub.Application.Events;
namespace SpaceHub.Application.Validators;

public sealed class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}
