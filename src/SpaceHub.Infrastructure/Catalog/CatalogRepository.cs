using Microsoft.EntityFrameworkCore;
using SpaceHub.Application.Catalog;
using SpaceHub.Application.Common;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Entities;
using SpaceHub.Infrastructure.Data;
namespace SpaceHub.Infrastructure.Catalog;

public sealed class CatalogRepository : ICatalogRepository
{
    private readonly AppDbContext db;

    public CatalogRepository(AppDbContext db)
    {
        this.db = db;
    }

    public Task<Space?> FindSpaceAsync(int id, CancellationToken ct) => db.SpaceRecords.SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<PagedResult<SpaceService>> ListServicesAsync(int spaceId, CatalogQuery query, bool includeInactive, CancellationToken ct)
    {
        var rows = db.SpaceServiceRecords.AsNoTracking().Where(x => x.SpaceId == spaceId && (includeInactive || x.IsActive));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            rows = rows.Where(x => x.Name.Contains(term));
        }
        var count = await rows.CountAsync(ct);
        var items = await rows.OrderBy(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Include(x => x.Space).ToListAsync(ct);
        return new(items, count, query.Page, query.PageSize);
    }
    public async Task<PagedResult<Package>> ListPackagesAsync(int spaceId, CatalogQuery query, bool includeInactive, CancellationToken ct)
    {
        var rows = db.PackageRecords.AsNoTracking().Where(x => x.SpaceId == spaceId && (includeInactive || x.IsActive && x.Items.All(i => i.Service.IsActive)));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            rows = rows.Where(x => x.Name.Contains(term));
        }
        var count = await rows.CountAsync(ct);
        var items = await rows.OrderBy(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Include(x => x.Space).Include(x => x.Items).ThenInclude(x => x.Service).AsSplitQuery().ToListAsync(ct);
        return new(items, count, query.Page, query.PageSize);
    }
    public Task<SpaceService?> FindServiceAsync(int spaceId, int id, CancellationToken ct)
        => db.SpaceServiceRecords.Include(x => x.Space).SingleOrDefaultAsync(x => x.SpaceId == spaceId && x.Id == id, ct);
    public Task<Package?> FindPackageAsync(int spaceId, int id, CancellationToken ct)
        => db.PackageRecords.Include(x => x.Space).Include(x => x.Items).ThenInclude(x => x.Service)
            .SingleOrDefaultAsync(x => x.SpaceId == spaceId && x.Id == id, ct);
    public Task<List<SpaceService>> FindServicesAsync(int spaceId, IReadOnlyList<int> ids, CancellationToken ct)
        => db.SpaceServiceRecords.Where(x => x.SpaceId == spaceId && ids.Contains(x.Id)).ToListAsync(ct);
    public Task<bool> HasPackageReferenceAsync(int id, bool activeOnly, CancellationToken ct)
        => db.PackageItemRecords.AnyAsync(x => x.SpaceServiceId == id && (!activeOnly || x.Package.IsActive), ct);
    public void AddService(SpaceService service) => db.SpaceServiceRecords.Add(service);
    public void AddPackage(Package package) => db.PackageRecords.Add(package);
    public void RemoveItem(PackageItem item) => db.PackageItemRecords.Remove(item);
    public async Task SaveAsync(Space space, byte[] expectedVersion, CancellationToken ct)
    {
        // All catalog edits participate in the same optimistic lock as their parent space.
        db.Entry(space).Property(x => x.RowVersion).OriginalValue = expectedVersion;
        db.Entry(space).Property(x => x.IsActive).IsModified = true;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException(FailureKind.Conflict, "Space catalog changed. Reload the space and retry with its latest rowVersion.");
        }
    }
}
