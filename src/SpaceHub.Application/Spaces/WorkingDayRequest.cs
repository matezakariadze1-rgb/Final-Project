namespace SpaceHub.Application.Spaces;

public sealed record WorkingDayRequest(DayOfWeek DayOfWeek, TimeOnly OpensAt, TimeOnly ClosesAt);
