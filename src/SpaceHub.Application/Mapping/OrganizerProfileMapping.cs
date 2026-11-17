using AutoMapper;
using SpaceHub.Application.Events;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Mapping;

public sealed class OrganizerProfileMapping : Profile
{
    public OrganizerProfileMapping()
    {
        CreateMap<OrganizerProfile, OrganizerProfileResponse>();
    }
}
