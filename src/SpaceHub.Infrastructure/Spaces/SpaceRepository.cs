using Microsoft.EntityFrameworkCore;
using SpaceHub.Application.Common;
using SpaceHub.Application.Exceptions;
using SpaceHub.Application.Spaces;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;
using SpaceHub.Infrastructure.Data;
namespace SpaceHub.Infrastructure.Spaces;

public sealed class SpaceRepository : ISpaceRepository
{
    private readonly AppDbContext db;

    public SpaceRepository(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<PagedResult<Space>> SearchAsync(SpaceQuery query, int? ownerId, CancellationToken ct)
    {
        var rows = db.SpaceRecords.AsNoTracking().Where(x => ownerId.HasValue ? x.OwnerId == ownerId.Value : x.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            rows = rows.Where(x => x.Name.Contains(term) || x.Description.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            rows = rows.Where(x => x.City == city);
        }
        if (query.MinCapacity.HasValue)
            rows = rows.Where(x => x.Capacity >= query.MinCapacity.Value);
        if (query.MaxHourlyRate.HasValue)
            rows = rows.Where(x => x.HourlyRate <= query.MaxHourlyRate.Value);
        var count = await rows.CountAsync(ct);
        var items = await rows.OrderBy(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Include(x => x.Owner).Include(x => x.WorkingHours).AsSplitQuery().ToListAsync(ct);
        return new(items, count, query.Page, query.PageSize);
    }
    public Task<Space?> FindAsync(int id, CancellationToken ct)
        => db.SpaceRecords.Include(x => x.Owner).Include(x => x.WorkingHours).SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task AddAsync(Space space, CancellationToken ct)
    {
        space.Owner = await db.UserRecords.FindAsync([space.OwnerId], ct)
            ?? throw new BusinessException(FailureKind.Unauthorized, "Account is not available.");
        db.SpaceRecords.Add(space);
        await db.SaveChangesAsync(ct);
    }
    public async Task SaveAsync(Space space, byte[] expectedVersion, CancellationToken ct)
    {
        db.Entry(space).Property(x => x.RowVersion).OriginalValue = expectedVersion;
        // Updating the parent protects working-hours changes as well as scalar changes.
        db.Entry(space).Property(x => x.IsActive).IsModified = true;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException(FailureKind.Conflict, "Space was changed by another request. Reload it and try again.");
        }
    }
    public async Task<IReadOnlyList<Booking>> GetFutureConfirmedBookingsAsync(int spaceId, CancellationToken ct)
        => await db.BookingRecords.AsNoTracking().Where(x => x.SpaceId == spaceId && x.Status == BookingStatus.Confirmed && x.EndsAt > DateTime.UtcNow).ToListAsync(ct);
    public void RemoveWorkingDay(SpaceWorkingHours day) => db.SpaceWorkingHoursRecords.Remove(day);
}
