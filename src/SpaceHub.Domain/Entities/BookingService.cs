using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class BookingService
{
    public int Id
    {
        get; set;
    }
    public int BookingId
    {
        get; set;
    }
    public int SpaceServiceId
    {
        get; set;
    }
    public string NameSnapshot { get; set; } = string.Empty;
    public ServicePricingType PricingTypeSnapshot
    {
        get; set;
    }
    public int Quantity
    {
        get; set;
    }
    public decimal UnitPriceSnapshot
    {
        get; set;
    }
    public decimal Subtotal
    {
        get; set;
    }
    public bool IsIncludedInPackage
    {
        get; set;
    }
    public Booking Booking { get; set; } = null!;
    public SpaceService Service { get; set; } = null!;
}
