using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class Event
{
    public int Id
    {
        get; set;
    }
    public int BookingId
    {
        get; set;
    }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartsAt
    {
        get; set;
    }
    public DateTime EndsAt
    {
        get; set;
    }
    public int MaxAttendees
    {
        get; set;
    }
    public decimal TicketPrice
    {
        get; set;
    }
    public EventVisibility Visibility
    {
        get; set;
    }
    public string? AccessCode
    {
        get; set;
    }
    public EventStatus Status
    {
        get; set;
    }
    public DateTime CreatedAt
    {
        get; set;
    }
    public byte[] RowVersion { get; set; } = [];
    public Booking Booking { get; set; } = null!;
    public ICollection<EventTag> EventTags { get; set; } = new List<EventTag>();
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
