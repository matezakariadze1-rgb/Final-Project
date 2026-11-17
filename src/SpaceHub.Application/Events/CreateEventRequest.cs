using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Events;

public sealed record CreateEventRequest(int BookingId, string Title, string Description, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    int MaxAttendees, decimal TicketPrice, EventVisibility Visibility, List<int> TagIds);
