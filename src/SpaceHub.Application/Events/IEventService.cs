using SpaceHub.Application.Catalog;
using SpaceHub.Application.Common;
namespace SpaceHub.Application.Events;

public interface IEventService
{
    Task<OrganizerProfileResponse> GetProfileAsync(int userId, CancellationToken ct);
    Task<OrganizerProfileResponse> UpsertProfileAsync(UpsertOrganizerProfileRequest request, int userId, CancellationToken ct);
    Task<PagedResult<TagResponse>> ListTagsAsync(CatalogQuery query, CancellationToken ct);
    Task<TagResponse> CreateTagAsync(CreateTagRequest request, CancellationToken ct);
    Task<PagedResult<EventListResponse>> ListAsync(EventQuery query, int? organizerId, CancellationToken ct);
    Task<EventDetailsResponse> GetAsync(int id, int userId, CancellationToken ct);
    Task<EventDetailsResponse> GetPrivateAsync(string code, int userId, CancellationToken ct);
    Task<EventDetailsResponse> CreateAsync(CreateEventRequest request, int userId, CancellationToken ct);
    Task<EventDetailsResponse> UpdateAsync(int id, UpdateEventRequest request, int userId, CancellationToken ct);
    Task<EventDetailsResponse> PublishAsync(int id, EventVersionRequest request, int userId, CancellationToken ct);
    Task<EventDetailsResponse> CancelAsync(int id, EventVersionRequest request, int userId, CancellationToken ct);
    Task DeleteAsync(int id, EventVersionRequest request, int userId, CancellationToken ct);
    Task RefreshDueAsync(CancellationToken ct);
}
