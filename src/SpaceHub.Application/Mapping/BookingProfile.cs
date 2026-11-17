using AutoMapper;
using SpaceHub.Application.Bookings;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Mapping;

public sealed class BookingProfile : Profile
{
    public BookingProfile()
    {
        CreateMap<BookingService, BookingLineResponse>();
        CreateMap<Booking, BookingQuoteResponse>()
            .ForMember(x => x.SpaceRowVersion, o => o.MapFrom(s => s.Space.RowVersion));
        CreateMap<Booking, BookingResponse>()
            .ForMember(x => x.SpaceName, o => o.MapFrom(s => s.Space.Name))
            .ForMember(x => x.CustomerName, o => o.MapFrom(s => s.User.FullName));
    }
}
