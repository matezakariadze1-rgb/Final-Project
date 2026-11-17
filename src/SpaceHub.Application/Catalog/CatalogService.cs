using AutoMapper;
using FluentValidation;
using SpaceHub.Application.Common;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Catalog;

public sealed class CatalogService : ICatalogService
{
    private readonly ICatalogRepository repository;
    private readonly IMapper mapper;
    private readonly IValidator<CreateServiceRequest> createServiceValidator;
    private readonly IValidator<UpdateServiceRequest> updateServiceValidator;
    private readonly IValidator<CreatePackageRequest> createPackageValidator;
    private readonly IValidator<UpdatePackageRequest> updatePackageValidator;
    private readonly IValidator<CatalogQuery> queryValidator;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public CatalogService(ICatalogRepository repository, IMapper mapper,
    IValidator<CreateServiceRequest> createServiceValidator, IValidator<UpdateServiceRequest> updateServiceValidator,
    IValidator<CreatePackageRequest> createPackageValidator, IValidator<UpdatePackageRequest> updatePackageValidator,
    IValidator<CatalogQuery> queryValidator)
    {
        this.repository = repository;
        this.mapper = mapper;
        this.createServiceValidator = createServiceValidator;
        this.updateServiceValidator = updateServiceValidator;
        this.createPackageValidator = createPackageValidator;
        this.updatePackageValidator = updatePackageValidator;
        this.queryValidator = queryValidator;
    }

    public async Task<PagedResult<ServiceResponse>> ListServicesAsync(int spaceId, CatalogQuery query, int userId, CancellationToken ct)
    {
        var space = await VisibleSpaceAsync(spaceId, userId, ct);
        await queryValidator.ValidateAndThrowAsync(query, ct);
        var result = await repository.ListServicesAsync(spaceId, query, space.OwnerId == userId, ct);
        return new(mapper.Map<List<ServiceResponse>>(result.Items), result.TotalCount, result.Page, result.PageSize);
    }
    public async Task<ServiceResponse> GetServiceAsync(int spaceId, int id, int userId, CancellationToken ct)
    {
        var space = await VisibleSpaceAsync(spaceId, userId, ct);
        var service = await repository.FindServiceAsync(spaceId, id, ct);
        if (service is null || !service.IsActive && space.OwnerId != userId)
            throw NotFound();
        return mapper.Map<ServiceResponse>(service);
    }
    public async Task<ServiceResponse> CreateServiceAsync(int spaceId, CreateServiceRequest request, int userId, CancellationToken ct)
    {
        var space = await OwnedSpaceAsync(spaceId, userId, ct);
        await createServiceValidator.ValidateAndThrowAsync(request, ct);
        var service = new SpaceService { Space = space, Name = request.Name.Trim(), PricingType = request.PricingType, UnitPrice = request.UnitPrice };
        repository.AddService(service);
        await repository.SaveAsync(space, request.SpaceRowVersion, ct);
        return mapper.Map<ServiceResponse>(service);
    }
    public async Task<ServiceResponse> UpdateServiceAsync(int spaceId, int id, UpdateServiceRequest request, int userId, CancellationToken ct)
    {
        var space = await OwnedSpaceAsync(spaceId, userId, ct);
        var service = await repository.FindServiceAsync(spaceId, id, ct) ?? throw NotFound();
        await updateServiceValidator.ValidateAndThrowAsync(request, ct);
        if (request.IsActive == false && await repository.HasPackageReferenceAsync(id, true, ct))
            throw new BusinessException(FailureKind.Validation, "Deactivate packages using this service before deactivating it.");
        if (request.PricingType != service.PricingType && await repository.HasPackageReferenceAsync(id, false, ct))
            throw new BusinessException(FailureKind.Validation, "Pricing type cannot change while the service belongs to a package.");
        service.Name = request.Name.Trim();
        service.UnitPrice = request.UnitPrice;
        service.PricingType = request.PricingType;
        service.IsActive = request.IsActive!.Value;
        await repository.SaveAsync(space, request.SpaceRowVersion, ct);
        return mapper.Map<ServiceResponse>(service);
    }
    public async Task<PagedResult<PackageResponse>> ListPackagesAsync(int spaceId, CatalogQuery query, int userId, CancellationToken ct)
    {
        var space = await VisibleSpaceAsync(spaceId, userId, ct);
        await queryValidator.ValidateAndThrowAsync(query, ct);
        var result = await repository.ListPackagesAsync(spaceId, query, space.OwnerId == userId, ct);
        return new(mapper.Map<List<PackageResponse>>(result.Items), result.TotalCount, result.Page, result.PageSize);
    }
    public async Task<PackageResponse> GetPackageAsync(int spaceId, int id, int userId, CancellationToken ct)
    {
        var space = await VisibleSpaceAsync(spaceId, userId, ct);
        var package = await repository.FindPackageAsync(spaceId, id, ct);
        if (package is null || space.OwnerId != userId && (!package.IsActive || package.Items.Any(x => !x.Service.IsActive)))
            throw NotFound();
        return mapper.Map<PackageResponse>(package);
    }
    public async Task<PackageResponse> CreatePackageAsync(int spaceId, CreatePackageRequest request, int userId, CancellationToken ct)
    {
        var space = await OwnedSpaceAsync(spaceId, userId, ct);
        await createPackageValidator.ValidateAndThrowAsync(request, ct);
        var services = await ValidateItemsAsync(spaceId, request.Items, true, ct);
        var package = new Package { Space = space, Name = request.Name.Trim(), Description = request.Description.Trim(), Price = request.Price };
        foreach (var item in request.Items)
            package.Items.Add(new PackageItem { Package = package, Service = services[item.SpaceServiceId], Quantity = item.Quantity });
        repository.AddPackage(package);
        await repository.SaveAsync(space, request.SpaceRowVersion, ct);
        return mapper.Map<PackageResponse>(package);
    }
    public async Task<PackageResponse> UpdatePackageAsync(int spaceId, int id, UpdatePackageRequest request, int userId, CancellationToken ct)
    {
        var space = await OwnedSpaceAsync(spaceId, userId, ct);
        var package = await repository.FindPackageAsync(spaceId, id, ct) ?? throw NotFound();
        await updatePackageValidator.ValidateAndThrowAsync(request, ct);
        var services = await ValidateItemsAsync(spaceId, request.Items, request.IsActive!.Value, ct);
        package.Name = request.Name.Trim();
        package.Description = request.Description.Trim();
        package.Price = request.Price;
        package.IsActive = request.IsActive.Value;
        foreach (var existing in package.Items.ToList())
        {
            var item = request.Items.SingleOrDefault(x => x.SpaceServiceId == existing.SpaceServiceId);
            if (item is null)
            {
                repository.RemoveItem(existing);
                package.Items.Remove(existing);
            }
            else
                existing.Quantity = item.Quantity;
        }
        foreach (var item in request.Items.Where(x => package.Items.All(i => i.SpaceServiceId != x.SpaceServiceId)))
            package.Items.Add(new PackageItem { Package = package, Service = services[item.SpaceServiceId], Quantity = item.Quantity });
        await repository.SaveAsync(space, request.SpaceRowVersion, ct);
        return mapper.Map<PackageResponse>(package);
    }
    private async Task<Dictionary<int, SpaceService>> ValidateItemsAsync(int spaceId, List<PackageItemRequest> items, bool requireActive, CancellationToken ct)
    {
        var services = await repository.FindServicesAsync(spaceId, items.Select(x => x.SpaceServiceId).ToArray(), ct);
        if (services.Count != items.Count || requireActive && services.Any(x => !x.IsActive))
            throw new BusinessException(FailureKind.Validation, "All services must belong to this space; active packages require active services.");
        return services.ToDictionary(x => x.Id);
    }
    private async Task<Space> VisibleSpaceAsync(int id, int userId, CancellationToken ct)
    {
        var space = await repository.FindSpaceAsync(id, ct);
        if (space is null || !space.IsActive && space.OwnerId != userId)
            throw NotFound();
        return space;
    }
    private async Task<Space> OwnedSpaceAsync(int id, int userId, CancellationToken ct)
    {
        var space = await repository.FindSpaceAsync(id, ct) ?? throw NotFound();
        if (space.OwnerId != userId)
            throw new BusinessException(FailureKind.Forbidden, "Only the space owner can manage its catalog.");
        return space;
    }
    private static BusinessException NotFound() => new(FailureKind.NotFound, "Resource was not found.");
}
