using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class EventTag
{
    public int EventId
    {
        get; set;
    }
    public int TagId
    {
        get; set;
    }
    public Event Event { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
