namespace SpaceHub.Application.Spaces;

public sealed record WorkingDayResponse(DayOfWeek DayOfWeek, TimeOnly OpensAt, TimeOnly ClosesAt);
