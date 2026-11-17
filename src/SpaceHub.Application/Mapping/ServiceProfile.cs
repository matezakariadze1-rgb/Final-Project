using AutoMapper;
using SpaceHub.Application.Catalog;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Mapping;

public sealed class ServiceProfile : Profile
{
    public ServiceProfile()
    {
        CreateMap<SpaceService, ServiceResponse>()
            .ForMember(x => x.Currency, o => o.MapFrom(s => "GEL"))
            .ForMember(x => x.SpaceRowVersion, o => o.MapFrom(s => s.Space.RowVersion));
    }
}
