namespace SpaceHub.Application.Bookings;

public sealed class AvailabilityQuery
{
    public DateTimeOffset StartsAt
    {
        get; init;
    }
    public DateTimeOffset EndsAt
    {
        get; init;
    }
    public int AttendeeCount { get; init; } = 1;
}
