using SpaceHub.Application.Catalog;
using SpaceHub.Application.Common;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Events;

public interface IEventRepository
{
    Task<OrganizerProfile?> GetProfileAsync(int userId, CancellationToken ct);
    Task<OrganizerProfile> UpsertProfileAsync(OrganizerProfile profile, CancellationToken ct);
    Task<PagedResult<TagResponse>> ListTagsAsync(CatalogQuery query, CancellationToken ct);
    Task<Tag> CreateTagAsync(string name, CancellationToken ct);
    Task<List<Tag>> GetTagsAsync(List<int> ids, CancellationToken ct);
    Task<PagedResult<EventListResponse>> ListAsync(EventQuery query, int? organizerId, CancellationToken ct);
    Task<Event?> FindAsync(int id, CancellationToken ct);
    Task<Event?> FindByCodeAsync(string code, CancellationToken ct);
    Task<int?> FindSpaceIdAsync(int id, CancellationToken ct);
    Task<bool> BookingHasEventAsync(int bookingId, CancellationToken ct);
    Task<bool> HasPublishedOverlapAsync(Event ev, CancellationToken ct);
    void Add(Event ev);
    void MarkChanged(Event ev);
    void Remove(Event ev);
    void RemoveTag(EventTag tag);
    Task<List<int>> DueSpaceIdsAsync(CancellationToken ct);
    Task<List<Event>> DueEventsAsync(int spaceId, CancellationToken ct);
}
