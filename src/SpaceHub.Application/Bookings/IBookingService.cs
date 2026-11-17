using SpaceHub.Application.Common;
namespace SpaceHub.Application.Bookings;

public interface IBookingService
{
    Task<AvailabilityResponse> AvailabilityAsync(int spaceId, AvailabilityQuery query, CancellationToken ct);
    Task<BookingQuoteResponse> QuoteAsync(BookingQuoteRequest request, CancellationToken ct);
    Task<BookingResponse> CreateAsync(CreateBookingRequest request, int userId, CancellationToken ct);
    Task<BookingResponse> GetAsync(int id, int userId, CancellationToken ct);
    Task<PagedResult<BookingResponse>> ListAsync(int userId, bool received, BookingQuery query, CancellationToken ct);
    Task<BookingResponse> ConfirmAsync(int id, ConfirmBookingRequest request, int userId, CancellationToken ct);
    Task<BookingResponse> RejectAsync(int id, RejectBookingRequest request, int userId, CancellationToken ct);
    Task<BookingResponse> CancelAsync(int id, CancelBookingRequest request, int userId, CancellationToken ct);
    Task RefreshDueAsync(CancellationToken ct);
}
