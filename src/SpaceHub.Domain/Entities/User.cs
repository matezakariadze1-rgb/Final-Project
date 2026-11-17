using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class User
{
    public int Id
    {
        get; set;
    }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt
    {
        get; set;
    }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public OrganizerProfile? OrganizerProfile
    {
        get; set;
    }
    public ICollection<Space> Spaces { get; set; } = new List<Space>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
