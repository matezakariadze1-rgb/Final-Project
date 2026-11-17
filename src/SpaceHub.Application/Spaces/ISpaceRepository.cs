using SpaceHub.Application.Common;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Spaces;

public interface ISpaceRepository
{
    Task<PagedResult<Space>> SearchAsync(SpaceQuery query, int? ownerId, CancellationToken ct);
    Task<Space?> FindAsync(int id, CancellationToken ct);
    Task AddAsync(Space space, CancellationToken ct);
    Task SaveAsync(Space space, byte[] expectedVersion, CancellationToken ct);
    Task<IReadOnlyList<Booking>> GetFutureConfirmedBookingsAsync(int spaceId, CancellationToken ct);
    void RemoveWorkingDay(SpaceWorkingHours day);
}
