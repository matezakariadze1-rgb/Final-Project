using SpaceHub.Application.Common;
namespace SpaceHub.Application.Registrations;

public interface IRegistrationService
{
    Task<RegistrationResponse> RegisterAsync(int eventId, RegisterForEventRequest request, int userId, CancellationToken ct);
    Task CancelAsync(int eventId, int userId, CancellationToken ct);
    Task<PagedResult<RegistrationResponse>> MineAsync(int userId, RegistrationQuery query, CancellationToken ct);
    Task<PagedResult<AttendeeResponse>> AttendeesAsync(int eventId, int organizerId, AttendeeQuery query, CancellationToken ct);
    Task<CheckInResponse> CheckInAsync(string code, int organizerId, CancellationToken ct);
}
