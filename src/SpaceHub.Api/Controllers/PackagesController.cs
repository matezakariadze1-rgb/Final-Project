using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceHub.Api.Extensions;
using SpaceHub.Application.Catalog;
using SpaceHub.Application.Common;
using SpaceHub.Domain.Constants;
namespace SpaceHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/spaces/{spaceId:int}/packages")]
public sealed class PackagesController : ControllerBase
{
    private readonly ICatalogService catalog;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public PackagesController(ICatalogService catalog)
    {
        this.catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PackageResponse>>> List(int spaceId, [FromQuery] CatalogQuery query, CancellationToken ct)
        => Ok(await catalog.ListPackagesAsync(spaceId, query, User.GetUserId(), ct));
    [HttpGet("{packageId:int}")]
    public async Task<ActionResult<PackageResponse>> Get(int spaceId, int packageId, CancellationToken ct)
        => Ok(await catalog.GetPackageAsync(spaceId, packageId, User.GetUserId(), ct));
    [HttpPost]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<PackageResponse>> Create(int spaceId, CreatePackageRequest request, CancellationToken ct)
    {
        var created = await catalog.CreatePackageAsync(spaceId, request, User.GetUserId(), ct);
        return CreatedAtAction(nameof(Get), new
        {
            spaceId,
            packageId = created.Id
        }, created);
    }
    [HttpPut("{packageId:int}")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<PackageResponse>> Update(int spaceId, int packageId, UpdatePackageRequest request, CancellationToken ct)
        => Ok(await catalog.UpdatePackageAsync(spaceId, packageId, request, User.GetUserId(), ct));
}
