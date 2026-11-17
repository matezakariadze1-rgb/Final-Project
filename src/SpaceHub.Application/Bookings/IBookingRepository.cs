using SpaceHub.Application.Common;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Bookings;

public interface IBookingRepository
{
    Task<Space?> LoadSpaceAsync(int spaceId, CancellationToken ct);
    Task<ISpaceBookingTransaction> BeginAsync(int spaceId, CancellationToken ct);
    Task<Booking?> FindAsync(int id, CancellationToken ct);
    Task<int?> FindSpaceIdAsync(int bookingId, CancellationToken ct);
    Task<List<Booking>> OverlappingAsync(int spaceId, DateTime start, DateTime end, BookingStatus status, CancellationToken ct);
    Task<PagedResult<Booking>> ListAsync(int userId, bool received, BookingQuery query, CancellationToken ct);
    Task<User?> FindUserAsync(int userId, CancellationToken ct);
    void Add(Booking booking);
    Task<List<int>> DueSpaceIdsAsync(CancellationToken ct);
    Task<List<Booking>> DueBookingsAsync(int spaceId, CancellationToken ct);
}
