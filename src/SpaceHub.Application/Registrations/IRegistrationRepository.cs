using SpaceHub.Application.Common;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Registrations;

public interface IRegistrationRepository
{
    Task<Event?> FindEventAsync(int eventId, CancellationToken ct);
    Task<Registration?> FindAsync(int eventId, int userId, CancellationToken ct);
    Task<int> CountConfirmedAsync(int eventId, CancellationToken ct);
    Task<PagedResult<RegistrationResponse>> MineAsync(int userId, RegistrationQuery query, CancellationToken ct);
    Task<PagedResult<AttendeeResponse>> AttendeesAsync(int eventId, AttendeeQuery query, CancellationToken ct);
    Task<int?> FindTicketEventIdAsync(string code, CancellationToken ct);
    Task<Ticket?> FindTicketAsync(string code, CancellationToken ct);
    Task LockTicketCodesAsync(CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);
    void Add(Registration registration);
}
