using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Bookings;

public interface ISpaceBookingTransaction : IAsyncDisposable
{
    Space Space
    {
        get;
    }
    Task CommitAsync(CancellationToken ct);
}
