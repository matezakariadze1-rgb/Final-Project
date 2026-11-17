namespace SpaceHub.Application.Bookings;

public sealed record CreateBookingRequest(int SpaceId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int AttendeeCount,
    int? PackageId, List<RequestedService> AdditionalServices, byte[] SpaceRowVersion);
