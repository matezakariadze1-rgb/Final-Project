using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Registrations;

public sealed class RegistrationResponse
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
    public RegistrationStatus Status
    {
        get; set;
    }
    public DateTime RegisteredAt
    {
        get; set;
    }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime StartsAt
    {
        get; set;
    }
    public DateTime EndsAt
    {
        get; set;
    }
    public EventStatus EventStatus
    {
        get; set;
    }
    public string SpaceName { get; set; } = string.Empty;
    public TicketResponse? Ticket
    {
        get; set;
    }
}
