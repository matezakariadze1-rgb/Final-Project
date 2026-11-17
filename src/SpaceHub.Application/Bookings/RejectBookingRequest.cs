namespace SpaceHub.Application.Bookings;

public sealed record RejectBookingRequest(string Reason, byte[] RowVersion);
