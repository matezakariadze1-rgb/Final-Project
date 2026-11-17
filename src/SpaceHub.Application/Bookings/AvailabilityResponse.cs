namespace SpaceHub.Application.Bookings;

public sealed record AvailabilityResponse(int SpaceId, bool IsAvailable, string? Reason);
