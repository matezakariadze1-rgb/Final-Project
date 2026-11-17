namespace SpaceHub.Application.Registrations;

public sealed record TicketResponse(string Code, DateTime IssuedAt, DateTime? CheckedInAt);
