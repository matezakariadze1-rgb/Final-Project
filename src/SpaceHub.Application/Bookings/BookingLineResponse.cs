using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Bookings;

public sealed class BookingLineResponse
{
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
}
