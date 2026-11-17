using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Events;

public class EventListResponse
{
    public int Id
    {
        get; set;
    }
    public string Title { get; set; } = string.Empty;
    public DateTime StartsAt
    {
        get; set;
    }
    public DateTime EndsAt
    {
        get; set;
    }
    public int OrganizerId
    {
        get; set;
    }
    public string OrganizerCompany { get; set; } = string.Empty;
    public string SpaceName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal TicketPrice
    {
        get; set;
    }
    public bool IsFree
    {
        get; set;
    }
    public int SeatsLeft
    {
        get; set;
    }
    public EventStatus Status
    {
        get; set;
    }
    public EventVisibility Visibility
    {
        get; set;
    }
    public List<TagResponse> Tags { get; set; } = [];
}
