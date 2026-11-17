using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceHub.Api.Extensions;
using SpaceHub.Application.Common;
using SpaceHub.Application.Events;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Constants;
namespace SpaceHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/events")]
public sealed class EventsController : ControllerBase
{
    private readonly IEventService events;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public EventsController(IEventService events)
    {
        this.events = events;
    }

    [HttpGet] public async Task<ActionResult<PagedResult<EventListResponse>>> List([FromQuery] EventQuery query, CancellationToken ct) => Ok(await events.ListAsync(query, null, ct));
    [HttpGet("/api/me/events")]
    [Authorize(Roles = RoleNames.Organizer)]
    public async Task<ActionResult<PagedResult<EventListResponse>>> Mine([FromQuery] EventQuery query, CancellationToken ct) => Ok(await events.ListAsync(query, User.GetUserId(), ct));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDetailsResponse>> Get(int id, CancellationToken ct) => Ok(await events.GetAsync(id, User.GetUserId(), ct));
    [HttpGet("/api/private-events/{accessCode}")]
    public async Task<ActionResult<EventDetailsResponse>> Private(string accessCode, CancellationToken ct) => Ok(await events.GetPrivateAsync(accessCode, User.GetUserId(), ct));
    [HttpPost]
    [Authorize(Roles = RoleNames.Organizer)]
    public async Task<ActionResult<EventDetailsResponse>> Create(CreateEventRequest request, CancellationToken ct)
    {
        var created = await events.CreateAsync(request, User.GetUserId(), ct);
        return CreatedAtAction(nameof(Get), new
        {
            id = created.Id
        }, created);
    }
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Organizer)]
    public async Task<ActionResult<EventDetailsResponse>> Update(int id, UpdateEventRequest request, CancellationToken ct) => Ok(await events.UpdateAsync(id, request, User.GetUserId(), ct));
    [HttpPost("{id:int}/publish")]
    [Authorize(Roles = RoleNames.Organizer)]
    public async Task<ActionResult<EventDetailsResponse>> Publish(int id, EventVersionRequest request, CancellationToken ct) => Ok(await events.PublishAsync(id, request, User.GetUserId(), ct));
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = RoleNames.Organizer)]
    public async Task<ActionResult<EventDetailsResponse>> Cancel(int id, EventVersionRequest request, CancellationToken ct) => Ok(await events.CancelAsync(id, request, User.GetUserId(), ct));
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Organizer)]
    public async Task<IActionResult> Delete(int id, [FromHeader(Name = "If-Match")] string? version, CancellationToken ct)
    {
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(version?.Trim('"') ?? "");
        }
        catch (FormatException) { throw new BusinessException(FailureKind.Validation, "If-Match must contain the event rowVersion in Base64."); }
        await events.DeleteAsync(id, new(bytes), User.GetUserId(), ct);
        return NoContent();
    }
}
