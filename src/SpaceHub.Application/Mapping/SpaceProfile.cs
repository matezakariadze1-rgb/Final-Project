using AutoMapper;
using SpaceHub.Application.Spaces;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Mapping;

public sealed class SpaceProfile : Profile
{
    public SpaceProfile()
    {
        CreateMap<Space, SpaceResponse>()
            .ForMember(x => x.OwnerName, o => o.MapFrom(s => s.Owner.FullName))
            .ForMember(x => x.Currency, o => o.MapFrom(s => "GEL"))
            .ForMember(x => x.WorkingDays, o => o.MapFrom(s => s.WorkingHours.Count));
        CreateMap<SpaceWorkingHours, WorkingDayResponse>();
    }
}
