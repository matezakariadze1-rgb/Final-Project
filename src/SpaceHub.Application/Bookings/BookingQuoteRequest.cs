namespace SpaceHub.Application.Bookings;

public sealed record BookingQuoteRequest(int SpaceId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int AttendeeCount,
    int? PackageId, List<RequestedService> AdditionalServices);
