namespace SpaceHub.Application.Bookings;

public sealed record CancelBookingRequest(string? Reason, byte[] RowVersion);
