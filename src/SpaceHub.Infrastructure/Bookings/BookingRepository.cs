using Microsoft.EntityFrameworkCore;
using SpaceHub.Application.Bookings;
using SpaceHub.Application.Common;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;
using SpaceHub.Infrastructure.Data;
namespace SpaceHub.Infrastructure.Bookings;

public sealed class BookingRepository : IBookingRepository
{
    private readonly AppDbContext db;

    public BookingRepository(AppDbContext db)
    {
        this.db = db;
    }

    public Task<Space?> LoadSpaceAsync(int spaceId, CancellationToken ct)
        => db.SpaceRecords.AsNoTracking().Include(x => x.WorkingHours).Include(x => x.Services)
            .Include(x => x.Packages).ThenInclude(x => x.Items).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == spaceId, ct);
    public async Task<ISpaceBookingTransaction> BeginAsync(int spaceId, CancellationToken ct)
    {
        var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
           
            var space = await db.SpaceRecords.FromSqlInterpolated($"SELECT * FROM [SpaceRecords] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {spaceId}")
                .SingleOrDefaultAsync(ct) ?? throw new BusinessException(FailureKind.NotFound, "Space was not found.");
            await db.Entry(space).Collection(x => x.WorkingHours).LoadAsync(ct);
            await db.Entry(space).Collection(x => x.Services).LoadAsync(ct);
            await db.Entry(space).Collection(x => x.Packages).Query().Include(x => x.Items).LoadAsync(ct);
            return new SpaceBookingTransaction(db, transaction, space);
        }
        catch { await transaction.DisposeAsync(); throw; }
    }
    public Task<Booking?> FindAsync(int id, CancellationToken ct)
        => db.BookingRecords.Include(x => x.Space).Include(x => x.User).Include(x => x.Services)
            .Include(x => x.Event).ThenInclude(x => x!.Registrations).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<int?> FindSpaceIdAsync(int id, CancellationToken ct)
        => db.BookingRecords.Where(x => x.Id == id).Select(x => (int?)x.SpaceId).SingleOrDefaultAsync(ct);
    public Task<List<Booking>> OverlappingAsync(int id, DateTime start, DateTime end, BookingStatus status, CancellationToken ct)
        => db.BookingRecords.Where(x => x.SpaceId == id && x.Status == status && x.StartsAt < end && x.EndsAt > start).ToListAsync(ct);
    public async Task<PagedResult<Booking>> ListAsync(int userId, bool received, BookingQuery query, CancellationToken ct)
    {
        var rows = db.BookingRecords.AsNoTracking().Where(x => received ? x.Space.OwnerId == userId : x.UserId == userId);
        if (query.Status.HasValue)
            rows = rows.Where(x => x.Status == query.Status.Value);
        var count = await rows.CountAsync(ct);
        var items = await rows.OrderByDescending(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Include(x => x.User).Include(x => x.Space).Include(x => x.Services).AsSplitQuery().ToListAsync(ct);
        return new(items, count, query.Page, query.PageSize);
    }
    public Task<User?> FindUserAsync(int id, CancellationToken ct) => db.UserRecords.SingleOrDefaultAsync(x => x.Id == id, ct);
    public void Add(Booking booking) => db.BookingRecords.Add(booking);
    public Task<List<int>> DueSpaceIdsAsync(CancellationToken ct) => db.BookingRecords
        .Where(x => x.Status == BookingStatus.Pending && x.StartsAt <= DateTime.UtcNow || x.Status == BookingStatus.Confirmed && x.EndsAt <= DateTime.UtcNow)
        .Select(x => x.SpaceId).Distinct().Order().Take(100).ToListAsync(ct);
    public Task<List<Booking>> DueBookingsAsync(int id, CancellationToken ct) => db.BookingRecords.Include(x => x.Event)
        .Where(x => x.SpaceId == id && (x.Status == BookingStatus.Pending && x.StartsAt <= DateTime.UtcNow || x.Status == BookingStatus.Confirmed && x.EndsAt <= DateTime.UtcNow)).ToListAsync(ct);
}
