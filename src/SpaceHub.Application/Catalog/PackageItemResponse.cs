using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Catalog;

public sealed class PackageItemResponse
{
    public int SpaceServiceId
    {
        get; set;
    }
    public string ServiceName { get; set; } = string.Empty;
    public ServicePricingType PricingType
    {
        get; set;
    }
    public int Quantity
    {
        get; set;
    }
    public bool IsActive
    {
        get; set;
    }
}
