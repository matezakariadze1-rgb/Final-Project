using System.Security.Cryptography;
using AutoMapper;
using FluentValidation;
using SpaceHub.Application.Bookings;
using SpaceHub.Application.Catalog;
using SpaceHub.Application.Common;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Events;

public sealed class EventAppService : IEventService
{
    private readonly IEventRepository events;
    private readonly IBookingRepository bookings;
    private readonly IMapper mapper;
    private readonly IValidator<UpsertOrganizerProfileRequest> profileValidator;
    private readonly IValidator<CreateTagRequest> tagValidator;
    private readonly IValidator<CatalogQuery> catalogQueryValidator;
    private readonly IValidator<EventQuery> queryValidator;
    private readonly IValidator<CreateEventRequest> createValidator;
    private readonly IValidator<UpdateEventRequest> updateValidator;
    private readonly IValidator<EventVersionRequest> versionValidator;

    
    public EventAppService(IEventRepository events, IBookingRepository bookings, IMapper mapper,
    IValidator<UpsertOrganizerProfileRequest> profileValidator, IValidator<CreateTagRequest> tagValidator,
    IValidator<CatalogQuery> catalogQueryValidator, IValidator<EventQuery> queryValidator,
    IValidator<CreateEventRequest> createValidator, IValidator<UpdateEventRequest> updateValidator,
    IValidator<EventVersionRequest> versionValidator)
    {
        this.events = events;
        this.bookings = bookings;
        this.mapper = mapper;
        this.profileValidator = profileValidator;
        this.tagValidator = tagValidator;
        this.catalogQueryValidator = catalogQueryValidator;
        this.queryValidator = queryValidator;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
        this.versionValidator = versionValidator;
    }

    public async Task<OrganizerProfileResponse> GetProfileAsync(int userId, CancellationToken ct)
        => mapper.Map<OrganizerProfileResponse>(await events.GetProfileAsync(userId, ct) ?? throw Missing());
    public async Task<OrganizerProfileResponse> UpsertProfileAsync(UpsertOrganizerProfileRequest request, int userId, CancellationToken ct)
    {
        await profileValidator.ValidateAndThrowAsync(request, ct);
        var profile = new OrganizerProfile
        {
            UserId = userId,
            CompanyName = request.CompanyName.Trim(),
            Phone = request.Phone,
            Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim(),
            Description = request.Description?.Trim()
        };
        return mapper.Map<OrganizerProfileResponse>(await events.UpsertProfileAsync(profile, ct));
    }
    public async Task<PagedResult<TagResponse>> ListTagsAsync(CatalogQuery query, CancellationToken ct)
    {
        await catalogQueryValidator.ValidateAndThrowAsync(query, ct);
        return await events.ListTagsAsync(query, ct);
    }
    public async Task<TagResponse> CreateTagAsync(CreateTagRequest request, CancellationToken ct)
    {
        await tagValidator.ValidateAndThrowAsync(request, ct);
        return mapper.Map<TagResponse>(await events.CreateTagAsync(request.Name.Trim(), ct));
    }
    public async Task<PagedResult<EventListResponse>> ListAsync(EventQuery query, int? organizerId, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct);
        return await events.ListAsync(query, organizerId, ct);
    }
    public async Task<EventDetailsResponse> GetAsync(int id, int userId, CancellationToken ct)
    {
        var ev = await events.FindAsync(id, ct) ?? throw Missing();
        if (ev.Booking.UserId != userId && (ev.Visibility != EventVisibility.Public || !IsPublished(ev)))
            throw Missing();
        return Details(ev, userId);
    }
    public async Task<EventDetailsResponse> GetPrivateAsync(string code, int userId, CancellationToken ct)
    {
        if (code.Length != 64 || !code.All(Uri.IsHexDigit))
            throw Missing();
        var ev = await events.FindByCodeAsync(code, ct) ?? throw Missing();
        if (ev.Visibility != EventVisibility.Private || ev.Booking.UserId != userId && !IsPublished(ev))
            throw Missing();
        return Details(ev, userId);
    }
    public async Task<EventDetailsResponse> CreateAsync(CreateEventRequest request, int userId, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        var spaceId = await bookings.FindSpaceIdAsync(request.BookingId, ct) ?? throw Missing();
        await using var transaction = await bookings.BeginAsync(spaceId, ct);
        var booking = await bookings.FindAsync(request.BookingId, ct) ?? throw Missing();
        if (booking.UserId != userId)
            throw Forbidden();
        if (await events.GetProfileAsync(userId, ct) is null)
            throw Invalid("Complete the organizer profile before creating an event.");
        if (await events.BookingHasEventAsync(booking.Id, ct))
            throw Conflict("This booking already has an event.");
        ValidateBooking(booking, request.StartsAt.UtcDateTime, request.EndsAt.UtcDateTime, request.MaxAttendees);
        var tags = await RequireTagsAsync(request.TagIds, ct);
        var ev = new Event
        {
            Booking = booking,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            StartsAt = request.StartsAt.UtcDateTime,
            EndsAt = request.EndsAt.UtcDateTime,
            MaxAttendees = request.MaxAttendees,
            TicketPrice = request.TicketPrice,
            Visibility = request.Visibility,
            AccessCode = request.Visibility == EventVisibility.Private ? NewCode() : null,
            CreatedAt = DateTime.UtcNow
        };
        foreach (var tag in tags)
            ev.EventTags.Add(new EventTag { Event = ev, Tag = tag });
        events.Add(ev);
        await transaction.CommitAsync(ct);
        return Details(ev, userId);
    }
    public async Task<EventDetailsResponse> UpdateAsync(int id, UpdateEventRequest request, int userId, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);
        await using var transaction = await BeginAsync(id, ct);
        var ev = await OwnedAsync(id, userId, request.RowVersion, ct);
        if (ev.Status is not (EventStatus.Draft or EventStatus.Published) || ev.StartsAt <= DateTime.UtcNow)
            throw Invalid("This event can no longer be edited.");
        ValidateBooking(ev.Booking, request.StartsAt.UtcDateTime, request.EndsAt.UtcDateTime, request.MaxAttendees);
        if (ev.Status == EventStatus.Published && ev.Registrations.Count > 0 &&
            (ev.StartsAt != request.StartsAt.UtcDateTime || ev.EndsAt != request.EndsAt.UtcDateTime || ev.Visibility != request.Visibility))
            throw Invalid("Time and visibility cannot change after registrations exist.");
        if (request.MaxAttendees < ev.Registrations.Count(x => x.Status == RegistrationStatus.Confirmed))
            throw Invalid("Capacity cannot be below confirmed registrations.");
        if (ev.Status == EventStatus.Published && request.TagIds.Count == 0)
            throw Invalid("Published events require at least one tag.");
        var tags = await RequireTagsAsync(request.TagIds, ct);
        ev.Title = request.Title.Trim();
        ev.Description = request.Description.Trim();
        ev.StartsAt = request.StartsAt.UtcDateTime;
        ev.EndsAt = request.EndsAt.UtcDateTime;
        ev.MaxAttendees = request.MaxAttendees;
        ev.TicketPrice = request.TicketPrice;
        if (ev.Visibility != request.Visibility)
            ev.AccessCode = request.Visibility == EventVisibility.Private ? NewCode() : null;
        ev.Visibility = request.Visibility;
        foreach (var old in ev.EventTags.Where(x => !request.TagIds.Contains(x.TagId)).ToList())
        {
            events.RemoveTag(old);
            ev.EventTags.Remove(old);
        }
        foreach (var tag in tags.Where(t => ev.EventTags.All(x => x.TagId != t.Id)))
            ev.EventTags.Add(new EventTag { Event = ev, Tag = tag });
        events.MarkChanged(ev); // Tag-only edits must also advance the event version.
        if (ev.Status == EventStatus.Published && await events.HasPublishedOverlapAsync(ev, ct))
            throw Conflict("Another published event overlaps this time in the space.");
        await transaction.CommitAsync(ct);
        return Details(ev, userId);
    }
    public async Task<EventDetailsResponse> PublishAsync(int id, EventVersionRequest request, int userId, CancellationToken ct)
    {
        await versionValidator.ValidateAndThrowAsync(request, ct);
        await using var transaction = await BeginAsync(id, ct);
        var ev = await OwnedAsync(id, userId, request.RowVersion, ct);
        if (ev.Status != EventStatus.Draft)
            throw Conflict("Only draft events can be published.");
        ValidateBooking(ev.Booking, ev.StartsAt, ev.EndsAt, ev.MaxAttendees);
        if (ev.StartsAt <= DateTime.UtcNow)
            throw Invalid("Event has already started.");
        if (await events.GetProfileAsync(userId, ct) is null || ev.EventTags.Count == 0)
            throw Invalid("Publishing requires an organizer profile and at least one tag.");
        if (await events.HasPublishedOverlapAsync(ev, ct))
            throw Conflict("Another published event overlaps this time in the space.");
        ev.Status = EventStatus.Published;
        await transaction.CommitAsync(ct);
        return Details(ev, userId);
    }
    public async Task<EventDetailsResponse> CancelAsync(int id, EventVersionRequest request, int userId, CancellationToken ct)
    {
        await versionValidator.ValidateAndThrowAsync(request, ct);
        await using var transaction = await BeginAsync(id, ct);
        var ev = await OwnedAsync(id, userId, request.RowVersion, ct);
        if (ev.Status is not (EventStatus.Draft or EventStatus.Published) || ev.EndsAt <= DateTime.UtcNow)
            throw Conflict("This event can no longer be cancelled.");
        ev.Status = EventStatus.Cancelled;
        foreach (var registration in ev.Registrations.Where(x => x.Status == RegistrationStatus.Confirmed))
            registration.Status = RegistrationStatus.Cancelled;
        await transaction.CommitAsync(ct);
        return Details(ev, userId);
    }
    public async Task DeleteAsync(int id, EventVersionRequest request, int userId, CancellationToken ct)
    {
        await versionValidator.ValidateAndThrowAsync(request, ct);
        await using var transaction = await BeginAsync(id, ct);
        var ev = await OwnedAsync(id, userId, request.RowVersion, ct);
        if (ev.Status != EventStatus.Draft || ev.Registrations.Count > 0)
            throw Invalid("Only a draft without registrations can be deleted.");
        events.Remove(ev);
        await transaction.CommitAsync(ct);
    }
    public async Task RefreshDueAsync(CancellationToken ct)
    {
        foreach (var id in await events.DueSpaceIdsAsync(ct))
        {
            await using var transaction = await bookings.BeginAsync(id, ct);
            var due = await events.DueEventsAsync(id, ct);
            if (due.Count == 0)
                continue;
            foreach (var ev in due)
                ev.Status = EventStatus.Completed;
            await transaction.CommitAsync(ct);
        }
    }
    private async Task<ISpaceBookingTransaction> BeginAsync(int id, CancellationToken ct)
        => await bookings.BeginAsync(await events.FindSpaceIdAsync(id, ct) ?? throw Missing(), ct);
    private async Task<Event> OwnedAsync(int id, int userId, byte[] version, CancellationToken ct)
    {
        var ev = await events.FindAsync(id, ct) ?? throw Missing();
        if (ev.Booking.UserId != userId)
            throw Forbidden();
        if (!ev.RowVersion.SequenceEqual(version))
            throw Conflict("Event changed. Reload it and retry.");
        return ev;
    }
    private async Task<List<Tag>> RequireTagsAsync(List<int> ids, CancellationToken ct)
    {
        var tags = await events.GetTagsAsync(ids, ct);
        if (tags.Count != ids.Count)
            throw Invalid("One or more tags do not exist.");
        return tags;
    }
    private EventDetailsResponse Details(Event ev, int userId)
    {
        var dto = mapper.Map<EventDetailsResponse>(ev);
        if (ev.Booking.UserId == userId)
            dto.AccessCode = ev.AccessCode;
        return dto;
    }
    private static bool IsPublished(Event ev) => ev.Status == EventStatus.Published && ev.EndsAt > DateTime.UtcNow && ev.Booking.Status == BookingStatus.Confirmed;
    private static void ValidateBooking(Booking booking, DateTime start, DateTime end, int capacity)
    {
        if (booking.Status != BookingStatus.Confirmed || booking.EndsAt <= DateTime.UtcNow)
            throw Invalid("A current confirmed booking is required.");
        if (start < booking.StartsAt || end > booking.EndsAt)
            throw Invalid("Event time must fit entirely inside the booking.");
        if (capacity > booking.AttendeeCount)
            throw Invalid("Event capacity cannot exceed the booked attendee count.");
    }
    private static string NewCode() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private static BusinessException Missing() => new(FailureKind.NotFound, "Resource was not found.");
    private static BusinessException Forbidden() => new(FailureKind.Forbidden, "Only the event organizer can manage this resource.");
    private static BusinessException Invalid(string message) => new(FailureKind.Validation, message);
    private static BusinessException Conflict(string message) => new(FailureKind.Conflict, message);
}
