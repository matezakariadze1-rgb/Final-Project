using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Catalog;

public sealed record UpdateServiceRequest(string Name, ServicePricingType PricingType, decimal UnitPrice, bool? IsActive, byte[] SpaceRowVersion);
