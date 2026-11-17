using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Catalog;

public sealed class ServiceResponse
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
    public string Currency { get; set; } = string.Empty;
    public bool IsActive
    {
        get; set;
    }
    public byte[] SpaceRowVersion { get; set; } = [];
}
