using FluentValidation;
using SpaceHub.Application.Catalog;
namespace SpaceHub.Application.Validators;

public sealed class CatalogQueryValidator : AbstractValidator<CatalogQuery>
{
    public CatalogQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(120);
        RuleFor(x => x.Page).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
