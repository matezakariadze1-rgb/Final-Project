using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class Registration
{
    public int Id
    {
        get; set;
    }
    public int EventId
    {
        get; set;
    }
    public int UserId
    {
        get; set;
    }
    public DateTime RegisteredAt
    {
        get; set;
    }
    public RegistrationStatus Status
    {
        get; set;
    }
    public Event Event { get; set; } = null!;
    public User User { get; set; } = null!;
    public Ticket? Ticket
    {
        get; set;
    }
}
