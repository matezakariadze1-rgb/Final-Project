using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceHub.Application.Catalog;
using SpaceHub.Application.Common;
using SpaceHub.Application.Events;
using SpaceHub.Domain.Constants;
namespace SpaceHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tags")]
public sealed class TagsController : ControllerBase
{
    private readonly IEventService events;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public TagsController(IEventService events)
    {
        this.events = events;
    }

    [HttpGet] public async Task<ActionResult<PagedResult<TagResponse>>> Get([FromQuery] CatalogQuery query, CancellationToken ct) => Ok(await events.ListTagsAsync(query, ct));
    [HttpPost]
    [Authorize(Roles = RoleNames.Organizer)]
    public async Task<ActionResult<TagResponse>> Post(CreateTagRequest request, CancellationToken ct) => StatusCode(201, await events.CreateTagAsync(request, ct));
}
