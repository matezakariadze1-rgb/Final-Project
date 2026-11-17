using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceHub.Api.Extensions;
using SpaceHub.Application.Events;
using SpaceHub.Domain.Constants;
namespace SpaceHub.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Organizer)]
[Route("api/me/organizer-profile")]
public sealed class OrganizerProfilesController : ControllerBase
{
    private readonly IEventService events;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public OrganizerProfilesController(IEventService events)
    {
        this.events = events;
    }

    [HttpGet] public async Task<ActionResult<OrganizerProfileResponse>> Get(CancellationToken ct) => Ok(await events.GetProfileAsync(User.GetUserId(), ct));
    [HttpPut] public async Task<ActionResult<OrganizerProfileResponse>> Put(UpsertOrganizerProfileRequest request, CancellationToken ct) => Ok(await events.UpsertProfileAsync(request, User.GetUserId(), ct));
}
