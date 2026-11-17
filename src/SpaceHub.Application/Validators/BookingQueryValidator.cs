using FluentValidation;
using SpaceHub.Application.Bookings;
namespace SpaceHub.Application.Validators;

public sealed class BookingQueryValidator : AbstractValidator<BookingQuery>
{
    public BookingQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Page).InclusiveBetween(1, 1000000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
