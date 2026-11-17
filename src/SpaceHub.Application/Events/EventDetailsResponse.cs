namespace SpaceHub.Application.Events;

public sealed class EventDetailsResponse : EventListResponse
{
    public int BookingId
    {
        get; set;
    }
    public string Description { get; set; } = string.Empty;
    public int MaxAttendees
    {
        get; set;
    }
    public DateTime CreatedAt
    {
        get; set;
    }
    public byte[] RowVersion { get; set; } = [];
    public string? AccessCode
    {
        get; set;
    }
}
