namespace SpaceHub.Application.Registrations;

public sealed record CheckInResponse(string TicketCode, int EventId, string EventTitle, string AttendeeName, DateTime CheckedInAt);
