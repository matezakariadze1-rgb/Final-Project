using FluentValidation;
using SpaceHub.Application.Spaces;
namespace SpaceHub.Application.Validators;

public sealed class SpaceQueryValidator : AbstractValidator<SpaceQuery>
{
    public SpaceQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(120);
        RuleFor(x => x.City).MaximumLength(60);
        RuleFor(x => x.MinCapacity).InclusiveBetween(1, 100000).When(x => x.MinCapacity.HasValue);
        RuleFor(x => x.MaxHourlyRate).InclusiveBetween(0m, 100000m).When(x => x.MaxHourlyRate.HasValue);
        RuleFor(x => x.Page).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
