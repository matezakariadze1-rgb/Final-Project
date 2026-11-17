namespace SpaceHub.Application.Bookings;

public sealed record RequestedService(int SpaceServiceId, int? Quantity);
