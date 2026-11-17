using AutoMapper;
using SpaceHub.Application.Events;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Mapping;

public sealed class EventProfile : Profile
{
    public EventProfile()
    {
        CreateMap<Tag, TagResponse>();
        CreateMap<Event, EventListResponse>()
            .ForMember(x => x.OrganizerId, o => o.MapFrom(s => s.Booking.UserId))
            .ForMember(x => x.OrganizerCompany, o => o.MapFrom(s => s.Booking.User.OrganizerProfile == null ? "" : s.Booking.User.OrganizerProfile.CompanyName))
            .ForMember(x => x.SpaceName, o => o.MapFrom(s => s.Booking.Space.Name))
            .ForMember(x => x.City, o => o.MapFrom(s => s.Booking.Space.City))
            .ForMember(x => x.IsFree, o => o.MapFrom(s => s.TicketPrice == 0m))
            .ForMember(x => x.SeatsLeft, o => o.MapFrom(s => s.MaxAttendees - s.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed)))
            .ForMember(x => x.Tags, o => o.MapFrom(s => s.EventTags.OrderBy(t => t.TagId).Select(t => t.Tag)));
        CreateMap<Event, EventDetailsResponse>().IncludeBase<Event, EventListResponse>()
            .ForMember(x => x.AccessCode, o => o.Ignore());
    }
}
