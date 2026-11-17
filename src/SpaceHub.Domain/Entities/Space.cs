using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class Space
{
    public int Id
    {
        get; set;
    }
    public int OwnerId
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Capacity
    {
        get; set;
    }
    public decimal HourlyRate
    {
        get; set;
    }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
    public User Owner { get; set; } = null!;
    public ICollection<SpaceWorkingHours> WorkingHours { get; set; } = new List<SpaceWorkingHours>();
    public ICollection<SpaceService> Services { get; set; } = new List<SpaceService>();
    public ICollection<Package> Packages { get; set; } = new List<Package>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
