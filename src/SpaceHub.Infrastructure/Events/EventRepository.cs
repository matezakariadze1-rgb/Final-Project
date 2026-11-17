using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SpaceHub.Application.Catalog;
using SpaceHub.Application.Common;
using SpaceHub.Application.Events;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;
using SpaceHub.Infrastructure.Data;
namespace SpaceHub.Infrastructure.Events;

public sealed class EventRepository : IEventRepository
{
    private readonly AppDbContext db;
    private readonly IMapper mapper;

    public EventRepository(AppDbContext db, IMapper mapper)
    {
        this.db = db;
        this.mapper = mapper;
    }

    public Task<OrganizerProfile?> GetProfileAsync(int userId, CancellationToken ct)
        => db.OrganizerProfileRecords.SingleOrDefaultAsync(x => x.UserId == userId, ct);
    public async Task<OrganizerProfile> UpsertProfileAsync(OrganizerProfile profile, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var user = await db.UserRecords.FromSqlInterpolated($"SELECT * FROM [UserRecords] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={profile.UserId}").SingleOrDefaultAsync(ct)
            ?? throw new BusinessException(FailureKind.Unauthorized, "Account is not available.");
        var current = await GetProfileAsync(user.Id, ct);
        if (current is null)
        {
            current = profile;
            db.OrganizerProfileRecords.Add(current);
        }
        else
        {
            current.CompanyName = profile.CompanyName;
            current.Phone = profile.Phone;
            current.Website = profile.Website;
            current.Description = profile.Description;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return current;
    }
    public async Task<PagedResult<TagResponse>> ListTagsAsync(CatalogQuery query, CancellationToken ct)
    {
        var rows = db.TagRecords.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            rows = rows.Where(x => x.Name.Contains(term));
        }
        var count = await rows.CountAsync(ct);
        return new(await rows.OrderBy(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ProjectTo<TagResponse>(mapper.ConfigurationProvider).ToListAsync(ct), count, query.Page, query.PageSize);
    }
    public async Task<Tag> CreateTagAsync(string name, CancellationToken ct)
    {
        var tag = new Tag { Name = name };
        db.TagRecords.Add(tag);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new BusinessException(FailureKind.Conflict, "A tag with this name already exists.");
        }
        return tag;
    }
    public Task<List<Tag>> GetTagsAsync(List<int> ids, CancellationToken ct) => db.TagRecords.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
    public async Task<PagedResult<EventListResponse>> ListAsync(EventQuery query, int? organizerId, CancellationToken ct)
    {
        var rows = db.EventRecords.AsNoTracking();
        rows = organizerId.HasValue ? rows.Where(x => x.Booking.UserId == organizerId) : rows.Where(x => x.Visibility == EventVisibility.Public && x.Status == EventStatus.Published && x.EndsAt > DateTime.UtcNow && x.Booking.Status == BookingStatus.Confirmed);
        if (query.Status.HasValue)
            rows = rows.Where(x => x.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            rows = rows.Where(x => x.Title.Contains(term) || x.Description.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            rows = rows.Where(x => x.Booking.Space.City == city);
        }
        if (query.TagId.HasValue)
            rows = rows.Where(x => x.EventTags.Any(t => t.TagId == query.TagId));
        if (query.From.HasValue)
        {
            var from = query.From.Value.UtcDateTime;
            rows = rows.Where(x => x.StartsAt >= from);
        }
        if (query.To.HasValue)
        {
            var to = query.To.Value.UtcDateTime;
            rows = rows.Where(x => x.StartsAt <= to);
        }
        if (query.FreeOnly)
            rows = rows.Where(x => x.TicketPrice == 0);
        var count = await rows.CountAsync(ct);
        var items = await rows.OrderBy(x => x.StartsAt).ThenBy(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ProjectTo<EventListResponse>(mapper.ConfigurationProvider).ToListAsync(ct);
        return new(items, count, query.Page, query.PageSize);
    }
    private IQueryable<Event> Details => db.EventRecords.Include(x => x.Booking).ThenInclude(x => x.Space)
        .Include(x => x.Booking).ThenInclude(x => x.User).ThenInclude(x => x.OrganizerProfile)
        .Include(x => x.EventTags).ThenInclude(x => x.Tag).Include(x => x.Registrations).AsSplitQuery();
    public Task<Event?> FindAsync(int id, CancellationToken ct) => Details.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Event?> FindByCodeAsync(string code, CancellationToken ct) => Details.SingleOrDefaultAsync(x => x.AccessCode == code, ct);
    public Task<int?> FindSpaceIdAsync(int id, CancellationToken ct) => db.EventRecords.Where(x => x.Id == id).Select(x => (int?)x.Booking.SpaceId).SingleOrDefaultAsync(ct);
    public Task<bool> BookingHasEventAsync(int id, CancellationToken ct) => db.EventRecords.AnyAsync(x => x.BookingId == id, ct);
    public Task<bool> HasPublishedOverlapAsync(Event ev, CancellationToken ct) => db.EventRecords.AnyAsync(x => x.Id != ev.Id && x.Booking.SpaceId == ev.Booking.SpaceId
        && x.Status == EventStatus.Published && x.StartsAt < ev.EndsAt && x.EndsAt > ev.StartsAt, ct);
    public void MarkChanged(Event ev) => db.Entry(ev).Property(x => x.Title).IsModified = true;
    public void Add(Event ev) => db.EventRecords.Add(ev);
    public void Remove(Event ev)
    {
        db.EventTagRecords.RemoveRange(ev.EventTags);
        db.EventRecords.Remove(ev);
    }
    public void RemoveTag(EventTag tag) => db.EventTagRecords.Remove(tag);
    public Task<List<int>> DueSpaceIdsAsync(CancellationToken ct) => db.EventRecords.Where(x => x.Status == EventStatus.Published && x.EndsAt <= DateTime.UtcNow)
        .Select(x => x.Booking.SpaceId).Distinct().Order().Take(100).ToListAsync(ct);
    public Task<List<Event>> DueEventsAsync(int spaceId, CancellationToken ct) => db.EventRecords.Where(x => x.Booking.SpaceId == spaceId && x.Status == EventStatus.Published && x.EndsAt <= DateTime.UtcNow).ToListAsync(ct);
}
