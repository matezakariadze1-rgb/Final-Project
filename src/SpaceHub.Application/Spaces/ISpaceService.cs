using SpaceHub.Application.Common;
namespace SpaceHub.Application.Spaces;

public interface ISpaceService
{
    Task<PagedResult<SpaceResponse>> SearchAsync(SpaceQuery query, int? ownerId, CancellationToken ct);
    Task<SpaceResponse> GetAsync(int id, int userId, CancellationToken ct);
    Task<SpaceResponse> CreateAsync(CreateSpaceRequest request, int userId, CancellationToken ct);
    Task<SpaceResponse> UpdateAsync(int id, UpdateSpaceRequest request, int userId, CancellationToken ct);
    Task<SpaceResponse> SetStatusAsync(int id, UpdateSpaceStatusRequest request, int userId, CancellationToken ct);
    Task<WorkingHoursResponse> GetWorkingHoursAsync(int id, int userId, CancellationToken ct);
    Task<WorkingHoursResponse> SetWorkingHoursAsync(int id, UpdateWorkingHoursRequest request, int userId, CancellationToken ct);
}
