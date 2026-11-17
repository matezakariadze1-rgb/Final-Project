using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Catalog;

public sealed record CreateServiceRequest(string Name, ServicePricingType PricingType, decimal UnitPrice, byte[] SpaceRowVersion);
