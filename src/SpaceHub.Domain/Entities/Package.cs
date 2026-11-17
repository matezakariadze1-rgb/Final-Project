using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class Package
{
    public int Id
    {
        get; set;
    }
    public int SpaceId
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price
    {
        get; set;
    }
    public bool IsActive { get; set; } = true;
    public Space Space { get; set; } = null!;
    public ICollection<PackageItem> Items { get; set; } = new List<PackageItem>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
