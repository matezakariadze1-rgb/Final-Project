using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SpaceHub.Application.Common;
using SpaceHub.Application.Exceptions;
using SpaceHub.Application.Registrations;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;
using SpaceHub.Infrastructure.Data;
namespace SpaceHub.Infrastructure.Registrations;

public sealed class RegistrationRepository : IRegistrationRepository
{
    private readonly AppDbContext db;
    private readonly IMapper mapper;

    public RegistrationRepository(AppDbContext db, IMapper mapper)
    {
        this.db = db;
        this.mapper = mapper;
    }

    public Task<Event?> FindEventAsync(int id, CancellationToken ct) => db.EventRecords.Include(x => x.Booking).ThenInclude(x => x.Space).SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Registration?> FindAsync(int eventId, int userId, CancellationToken ct) => db.RegistrationRecords.Include(x => x.User).Include(x => x.Ticket)
        .Include(x => x.Event).ThenInclude(x => x.Booking).ThenInclude(x => x.Space).SingleOrDefaultAsync(x => x.EventId == eventId && x.UserId == userId, ct);
    public Task<int> CountConfirmedAsync(int eventId, CancellationToken ct) => db.RegistrationRecords.CountAsync(x => x.EventId == eventId && x.Status == RegistrationStatus.Confirmed, ct);
    public async Task<PagedResult<RegistrationResponse>> MineAsync(int userId, RegistrationQuery query, CancellationToken ct)
    {
        var rows = db.RegistrationRecords.AsNoTracking().Where(x => x.UserId == userId);
        if (query.Status.HasValue)
            rows = rows.Where(x => x.Status == query.Status.Value);
        if (query.UpcomingOnly)
            rows = rows.Where(x => x.Status == RegistrationStatus.Confirmed && x.Event.Status == EventStatus.Published
            && x.Event.Booking.Status == BookingStatus.Confirmed && x.Event.StartsAt > DateTime.UtcNow);
        var count = await rows.CountAsync(ct);
        var items = await rows.OrderByDescending(x => x.RegisteredAt).ThenByDescending(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ProjectTo<RegistrationResponse>(mapper.ConfigurationProvider).ToListAsync(ct);
        return new(items, count, query.Page, query.PageSize);
    }
    public async Task<PagedResult<AttendeeResponse>> AttendeesAsync(int eventId, AttendeeQuery query, CancellationToken ct)
    {
        var rows = db.RegistrationRecords.AsNoTracking().Where(x => x.EventId == eventId);
        if (query.Status.HasValue)
            rows = rows.Where(x => x.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            rows = rows.Where(x => x.User.FullName.Contains(term) || x.User.Email.Contains(term));
        }
        var count = await rows.CountAsync(ct);
        var items = await rows.OrderBy(x => x.RegisteredAt).ThenBy(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ProjectTo<AttendeeResponse>(mapper.ConfigurationProvider).ToListAsync(ct);
        return new(items, count, query.Page, query.PageSize);
    }
    public Task<int?> FindTicketEventIdAsync(string code, CancellationToken ct) => db.TicketRecords.Where(x => x.Code == code).Select(x => (int?)x.Registration.EventId).SingleOrDefaultAsync(ct);
    public Task<Ticket?> FindTicketAsync(string code, CancellationToken ct) => db.TicketRecords.Include(x => x.Registration).ThenInclude(x => x.User)
        .Include(x => x.Registration).ThenInclude(x => x.Event).ThenInclude(x => x.Booking).SingleOrDefaultAsync(x => x.Code == code, ct);
    public async Task LockTicketCodesAsync(CancellationToken ct)
    {
        // A short transaction-scoped allocation lock closes the check/insert race across different spaces.
        try
        {
            await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sp_getapplock @Resource=N'SpaceHub.TicketCodes', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r < 0 THROW 51000, 'Ticket allocation is busy.', 1;", ct);
        }
        catch (SqlException ex) when (ex.Number == 51000)
        {
            throw new BusinessException(FailureKind.Conflict, "Ticket allocation is busy. Retry the request.");
        }
    }
    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) => db.TicketRecords.AnyAsync(x => x.Code == code, ct);
    public void Add(Registration registration) => db.RegistrationRecords.Add(registration);
}
