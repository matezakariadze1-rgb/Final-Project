using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class SpaceService
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
    public ServicePricingType PricingType
    {
        get; set;
    }
    public decimal UnitPrice
    {
        get; set;
    }
    public bool IsActive { get; set; } = true;
    public Space Space { get; set; } = null!;
    public ICollection<PackageItem> PackageItems { get; set; } = new List<PackageItem>();
    public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
}
