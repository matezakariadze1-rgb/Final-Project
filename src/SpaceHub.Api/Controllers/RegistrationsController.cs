using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceHub.Api.Extensions;
using SpaceHub.Application.Common;
using SpaceHub.Application.Registrations;
using SpaceHub.Domain.Constants;
namespace SpaceHub.Api.Controllers;

[ApiController]
[Authorize]
public sealed class RegistrationsController : ControllerBase
{
    private readonly IRegistrationService registrations;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public RegistrationsController(IRegistrationService registrations)
    {
        this.registrations = registrations;
    }

    [HttpPost("api/events/{eventId:int}/registrations")]
    [Authorize(Roles = RoleNames.Attendee)]
    public async Task<ActionResult<RegistrationResponse>> Register(int eventId, RegisterForEventRequest request, CancellationToken ct)
        => StatusCode(201, await registrations.RegisterAsync(eventId, request, User.GetUserId(), ct));
    [HttpDelete("api/events/{eventId:int}/registrations")]
    [Authorize(Roles = RoleNames.Attendee)]
    public async Task<IActionResult> Cancel(int eventId, CancellationToken ct)
    {
        await registrations.CancelAsync(eventId, User.GetUserId(), ct);
        return NoContent();
    }
    [HttpGet("api/me/registrations")]
    [Authorize(Roles = RoleNames.Attendee)]
    public async Task<ActionResult<PagedResult<RegistrationResponse>>> Mine([FromQuery] RegistrationQuery query, CancellationToken ct)
        => Ok(await registrations.MineAsync(User.GetUserId(), query, ct));
    [HttpGet("api/events/{eventId:int}/attendees")]
    [Authorize(Roles = RoleNames.Organizer)]
    public async Task<ActionResult<PagedResult<AttendeeResponse>>> Attendees(int eventId, [FromQuery] AttendeeQuery query, CancellationToken ct)
        => Ok(await registrations.AttendeesAsync(eventId, User.GetUserId(), query, ct));
    [HttpPost("api/tickets/{code}/check-in")]
    [Authorize(Roles = RoleNames.Organizer)]
    public async Task<ActionResult<CheckInResponse>> CheckIn(string code, CancellationToken ct)
        => Ok(await registrations.CheckInAsync(code, User.GetUserId(), ct));
}
