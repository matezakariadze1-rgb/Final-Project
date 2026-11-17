using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Bookings;

public sealed class BookingQuery
{
    public BookingStatus? Status
    {
        get; init;
    }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
