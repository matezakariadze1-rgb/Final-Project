using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceHub.Api.Extensions;
using SpaceHub.Application.Common;
using SpaceHub.Application.Spaces;
using SpaceHub.Domain.Constants;
namespace SpaceHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/spaces")]
public sealed class SpacesController : ControllerBase
{
    private readonly ISpaceService spaces;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public SpacesController(ISpaceService spaces)
    {
        this.spaces = spaces;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SpaceResponse>>> Search([FromQuery] SpaceQuery query, CancellationToken ct)
        => Ok(await spaces.SearchAsync(query, null, ct));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SpaceResponse>> Get(int id, CancellationToken ct)
        => Ok(await spaces.GetAsync(id, User.GetUserId(), ct));
    [HttpGet("/api/me/spaces")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<PagedResult<SpaceResponse>>> Mine([FromQuery] SpaceQuery query, CancellationToken ct)
        => Ok(await spaces.SearchAsync(query, User.GetUserId(), ct));
    [HttpPost]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<SpaceResponse>> Create(CreateSpaceRequest request, CancellationToken ct)
    {
        var created = await spaces.CreateAsync(request, User.GetUserId(), ct);
        return CreatedAtAction(nameof(Get), new
        {
            id = created.Id
        }, created);
    }
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<SpaceResponse>> Update(int id, UpdateSpaceRequest request, CancellationToken ct)
        => Ok(await spaces.UpdateAsync(id, request, User.GetUserId(), ct));
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<SpaceResponse>> Status(int id, UpdateSpaceStatusRequest request, CancellationToken ct)
        => Ok(await spaces.SetStatusAsync(id, request, User.GetUserId(), ct));
    [HttpGet("{id:int}/working-hours")]
    public async Task<ActionResult<WorkingHoursResponse>> Hours(int id, CancellationToken ct)
        => Ok(await spaces.GetWorkingHoursAsync(id, User.GetUserId(), ct));
    [HttpPut("{id:int}/working-hours")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<WorkingHoursResponse>> SetHours(int id, UpdateWorkingHoursRequest request, CancellationToken ct)
        => Ok(await spaces.SetWorkingHoursAsync(id, request, User.GetUserId(), ct));
}
