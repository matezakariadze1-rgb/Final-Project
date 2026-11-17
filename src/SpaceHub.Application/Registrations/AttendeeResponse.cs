using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Registrations;

public sealed class AttendeeResponse
{
    public int Id
    {
        get; set;
    }
    public int UserId
    {
        get; set;
    }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RegisteredAt
    {
        get; set;
    }
    public RegistrationStatus Status
    {
        get; set;
    }
    public string? TicketCode
    {
        get; set;
    }
    public bool IsCheckedIn
    {
        get; set;
    }
}
