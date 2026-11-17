using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceHub.Api.Extensions;
using SpaceHub.Application.Bookings;
using SpaceHub.Application.Common;
using SpaceHub.Domain.Constants;
namespace SpaceHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly IBookingService bookings;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public BookingsController(IBookingService bookings)
    {
        this.bookings = bookings;
    }

    [HttpGet("/api/spaces/{spaceId:int}/availability")]
    public async Task<ActionResult<AvailabilityResponse>> Availability(int spaceId, [FromQuery] AvailabilityQuery query, CancellationToken ct)
        => Ok(await bookings.AvailabilityAsync(spaceId, query, ct));
    [HttpPost("/api/booking-quotes")]
    public async Task<ActionResult<BookingQuoteResponse>> Quote(BookingQuoteRequest request, CancellationToken ct)
        => Ok(await bookings.QuoteAsync(request, ct));
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(CreateBookingRequest request, CancellationToken ct)
    {
        var created = await bookings.CreateAsync(request, User.GetUserId(), ct);
        return CreatedAtAction(nameof(Get), new
        {
            id = created.Id
        }, created);
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingResponse>> Get(int id, CancellationToken ct)
        => Ok(await bookings.GetAsync(id, User.GetUserId(), ct));
    [HttpGet("/api/me/bookings")]
    public async Task<ActionResult<PagedResult<BookingResponse>>> Mine([FromQuery] BookingQuery query, CancellationToken ct)
        => Ok(await bookings.ListAsync(User.GetUserId(), false, query, ct));
    [HttpGet("/api/me/received-bookings")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<PagedResult<BookingResponse>>> Received([FromQuery] BookingQuery query, CancellationToken ct)
        => Ok(await bookings.ListAsync(User.GetUserId(), true, query, ct));
    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<BookingResponse>> Confirm(int id, ConfirmBookingRequest request, CancellationToken ct)
        => Ok(await bookings.ConfirmAsync(id, request, User.GetUserId(), ct));
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = RoleNames.Owner)]
    public async Task<ActionResult<BookingResponse>> Reject(int id, RejectBookingRequest request, CancellationToken ct)
        => Ok(await bookings.RejectAsync(id, request, User.GetUserId(), ct));
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<BookingResponse>> Cancel(int id, CancelBookingRequest request, CancellationToken ct)
        => Ok(await bookings.CancelAsync(id, request, User.GetUserId(), ct));
}
