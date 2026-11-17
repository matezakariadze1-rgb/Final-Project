using FluentValidation;
using SpaceHub.Application.Spaces;
namespace SpaceHub.Application.Validators;

public sealed class UpdateWorkingHoursRequestValidator : AbstractValidator<UpdateWorkingHoursRequest>
{
    public UpdateWorkingHoursRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotNull().Must(x => x is { Length: 8 }).WithMessage("A valid RowVersion is required.");
        RuleFor(x => x.Days).NotNull().Must(x => x is null || x.Count <= 7).WithMessage("At most 7 days are allowed.")
            .Must(x => x is null || x.All(d => d is not null) && x.Select(d => d.DayOfWeek).Distinct().Count() == x.Count)
            .WithMessage("Days must be non-null and unique.");
        RuleForEach(x => x.Days).SetValidator(new WorkingDayRequestValidator());
    }
}
