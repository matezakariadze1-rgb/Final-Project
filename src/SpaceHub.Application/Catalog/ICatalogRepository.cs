using SpaceHub.Application.Common;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Catalog;

public interface ICatalogRepository
{
    Task<Space?> FindSpaceAsync(int id, CancellationToken ct);
    Task<PagedResult<SpaceService>> ListServicesAsync(int spaceId, CatalogQuery query, bool includeInactive, CancellationToken ct);
    Task<PagedResult<Package>> ListPackagesAsync(int spaceId, CatalogQuery query, bool includeInactive, CancellationToken ct);
    Task<SpaceService?> FindServiceAsync(int spaceId, int serviceId, CancellationToken ct);
    Task<Package?> FindPackageAsync(int spaceId, int packageId, CancellationToken ct);
    Task<List<SpaceService>> FindServicesAsync(int spaceId, IReadOnlyList<int> ids, CancellationToken ct);
    Task<bool> HasPackageReferenceAsync(int serviceId, bool activeOnly, CancellationToken ct);
    void AddService(SpaceService service);
    void AddPackage(Package package);
    void RemoveItem(PackageItem item);
    Task SaveAsync(Space space, byte[] expectedVersion, CancellationToken ct);
}
