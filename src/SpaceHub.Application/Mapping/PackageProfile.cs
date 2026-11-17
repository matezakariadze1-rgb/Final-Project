using AutoMapper;
using SpaceHub.Application.Catalog;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Mapping;

public sealed class PackageProfile : Profile
{
    public PackageProfile()
    {
        CreateMap<Package, PackageResponse>()
            .ForMember(x => x.Currency, o => o.MapFrom(s => "GEL"))
            .ForMember(x => x.Items, o => o.MapFrom(s => s.Items.OrderBy(i => i.SpaceServiceId)))
            .ForMember(x => x.SpaceRowVersion, o => o.MapFrom(s => s.Space.RowVersion));
        CreateMap<PackageItem, PackageItemResponse>()
            .ForMember(x => x.ServiceName, o => o.MapFrom(s => s.Service.Name))
            .ForMember(x => x.PricingType, o => o.MapFrom(s => s.Service.PricingType))
            .ForMember(x => x.IsActive, o => o.MapFrom(s => s.Service.IsActive));
    }
}
