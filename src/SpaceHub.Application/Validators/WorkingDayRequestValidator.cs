using FluentValidation;
using SpaceHub.Application.Spaces;
namespace SpaceHub.Application.Validators;

public sealed class WorkingDayRequestValidator : AbstractValidator<WorkingDayRequest>
{
    public WorkingDayRequestValidator()
    {
        RuleFor(x => x.DayOfWeek).IsInEnum();
        RuleFor(x => x.ClosesAt).GreaterThan(x => x.OpensAt);
        RuleFor(x => x.OpensAt).Must(IsHalfHour).WithMessage("Use 30-minute intervals.");
        RuleFor(x => x.ClosesAt).Must(IsHalfHour).WithMessage("Use 30-minute intervals.");
    }
    private static bool IsHalfHour(TimeOnly value) => value.Ticks % TimeSpan.FromMinutes(30).Ticks == 0;
}
