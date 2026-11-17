using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceHub.Api.Extensions;
using SpaceHub.Application.Catalog;
using SpaceHub.Application.Common;
using SpaceHub.Domain.Constants;
namespace SpaceHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/spaces/{spaceId:int}/services")]
public sealed class SpaceServicesController : ControllerBase
{
    private readonly ICatalogService catalog;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public SpaceServicesController(ICatalogService catalog)
    {
        this.catalog = catalog;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ServiceResponse>>> List(int spaceId, [FromQuery] CatalogQuery query, CancellationToken ct)
        => Ok(await catalog.ListServicesAsync(spaceId, query, User.GetUserId(), ct));
    [HttpGet("{serviceId:int}")]
    public async Task<ActionResult<ServiceResponse>> Get(int spaceId, int serviceId, CancellationToken ct)
        => Ok(await catalog.GetServiceAsync(spaceId, serviceId, User.GetUserId(), ct));
    [HttpPost]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<ServiceResponse>> Create(int spaceId, CreateServiceRequest request, CancellationToken ct)
    {
        var created = await catalog.CreateServiceAsync(spaceId, request, User.GetUserId(), ct);
        return CreatedAtAction(nameof(Get), new
        {
            spaceId,
            serviceId = created.Id
        }, created);
    }
    [HttpPut("{serviceId:int}")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<ServiceResponse>> Update(int spaceId, int serviceId, UpdateServiceRequest request, CancellationToken ct)
        => Ok(await catalog.UpdateServiceAsync(spaceId, serviceId, request, User.GetUserId(), ct));
}
