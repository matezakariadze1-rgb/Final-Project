using SpaceHub.Application.Common;
namespace SpaceHub.Application.Catalog;

public interface ICatalogService
{
    Task<PagedResult<ServiceResponse>> ListServicesAsync(int spaceId, CatalogQuery query, int userId, CancellationToken ct);
    Task<ServiceResponse> GetServiceAsync(int spaceId, int id, int userId, CancellationToken ct);
    Task<ServiceResponse> CreateServiceAsync(int spaceId, CreateServiceRequest request, int userId, CancellationToken ct);
    Task<ServiceResponse> UpdateServiceAsync(int spaceId, int id, UpdateServiceRequest request, int userId, CancellationToken ct);
    Task<PagedResult<PackageResponse>> ListPackagesAsync(int spaceId, CatalogQuery query, int userId, CancellationToken ct);
    Task<PackageResponse> GetPackageAsync(int spaceId, int id, int userId, CancellationToken ct);
    Task<PackageResponse> CreatePackageAsync(int spaceId, CreatePackageRequest request, int userId, CancellationToken ct);
    Task<PackageResponse> UpdatePackageAsync(int spaceId, int id, UpdatePackageRequest request, int userId, CancellationToken ct);
}
