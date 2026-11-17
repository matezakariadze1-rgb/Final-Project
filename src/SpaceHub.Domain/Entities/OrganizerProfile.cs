using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class OrganizerProfile
{
    public int Id
    {
        get; set;
    }
    public int UserId
    {
        get; set;
    }
    public string CompanyName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Website
    {
        get; set;
    }
    public string? Description
    {
        get; set;
    }
    public bool IsVerified
    {
        get; set;
    }
    public User User { get; set; } = null!;
}
