using AutoMapper;
using SpaceHub.Application.Registrations;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Mapping;

public sealed class RegistrationProfile : Profile
{
    public RegistrationProfile()
    {
        CreateMap<Ticket, TicketResponse>();
        CreateMap<Registration, RegistrationResponse>()
            .ForMember(x => x.EventTitle, o => o.MapFrom(s => s.Event.Title))
            .ForMember(x => x.StartsAt, o => o.MapFrom(s => s.Event.StartsAt))
            .ForMember(x => x.EndsAt, o => o.MapFrom(s => s.Event.EndsAt))
            .ForMember(x => x.EventStatus, o => o.MapFrom(s => s.Event.Status))
            .ForMember(x => x.SpaceName, o => o.MapFrom(s => s.Event.Booking.Space.Name));
        CreateMap<Registration, AttendeeResponse>()
            .ForMember(x => x.FullName, o => o.MapFrom(s => s.User.FullName))
            .ForMember(x => x.Email, o => o.MapFrom(s => s.User.Email))
            .ForMember(x => x.TicketCode, o => o.MapFrom(s => s.Ticket == null ? null : s.Ticket.Code))
            .ForMember(x => x.IsCheckedIn, o => o.MapFrom(s => s.Ticket != null && s.Ticket.CheckedInAt != null));
    }
}
