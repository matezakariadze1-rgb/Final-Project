using System.Security.Cryptography;
using AutoMapper;
using FluentValidation;
using SpaceHub.Application.Bookings;
using SpaceHub.Application.Common;
using SpaceHub.Application.Events;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Registrations;

public sealed class RegistrationAppService : IRegistrationService
{
    private readonly IRegistrationRepository registrations;
    private readonly IEventRepository events;
    private readonly IBookingRepository bookings;
    private readonly IMapper mapper;
    private readonly IValidator<RegisterForEventRequest> requestValidator;
    private readonly IValidator<RegistrationQuery> queryValidator;
    private readonly IValidator<AttendeeQuery> attendeeValidator;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public RegistrationAppService(IRegistrationRepository registrations, IEventRepository events, IBookingRepository bookings, IMapper mapper,
    IValidator<RegisterForEventRequest> requestValidator, IValidator<RegistrationQuery> queryValidator, IValidator<AttendeeQuery> attendeeValidator)
    {
        this.registrations = registrations;
        this.events = events;
        this.bookings = bookings;
        this.mapper = mapper;
        this.requestValidator = requestValidator;
        this.queryValidator = queryValidator;
        this.attendeeValidator = attendeeValidator;
    }

    public async Task<RegistrationResponse> RegisterAsync(int eventId, RegisterForEventRequest request, int userId, CancellationToken ct)
    {
        await requestValidator.ValidateAndThrowAsync(request, ct);
        await using var transaction = await BeginAsync(eventId, ct);
        var ev = await registrations.FindEventAsync(eventId, ct) ?? throw Missing();
        if (ev.Visibility == EventVisibility.Private && !string.Equals(ev.AccessCode, request.AccessCode, StringComparison.Ordinal))
            throw Missing();
        if (ev.Status != EventStatus.Published || ev.Booking.Status != BookingStatus.Confirmed || ev.StartsAt <= DateTime.UtcNow)
            throw Invalid("Registration requires a published, not-yet-started event on a confirmed booking.");
        var registration = await registrations.FindAsync(eventId, userId, ct);
        if (registration is { Status: RegistrationStatus.Confirmed })
            throw Conflict("You are already registered for this event.");
        if (await registrations.CountConfirmedAsync(eventId, ct) >= ev.MaxAttendees)
            throw Invalid("No seats are available.");
        var user = await bookings.FindUserAsync(userId, ct) ?? throw new BusinessException(FailureKind.Unauthorized, "Account is not available.");
        await registrations.LockTicketCodesAsync(ct);
        var code = await NewCodeAsync(ct);
        var now = DateTime.UtcNow;
        if (registration is null)
        {
            registration = new Registration { Event = ev, User = user, RegisteredAt = now, Status = RegistrationStatus.Confirmed };
            registration.Ticket = new Ticket { Registration = registration, Code = code, IssuedAt = now };
            registrations.Add(registration);
        }
        else
        {
            registration.Status = RegistrationStatus.Confirmed;
            registration.RegisteredAt = now;
            if (registration.Ticket is null)
                registration.Ticket = new Ticket { Registration = registration, Code = code, IssuedAt = now };
            else
            {
                registration.Ticket.Code = code;
                registration.Ticket.IssuedAt = now;
                registration.Ticket.CheckedInAt = null;
            }
        }
        events.MarkChanged(ev);
        await transaction.CommitAsync(ct);
        return mapper.Map<RegistrationResponse>(registration);
    }
    public async Task CancelAsync(int eventId, int userId, CancellationToken ct)
    {
        await using var transaction = await BeginAsync(eventId, ct);
        var registration = await registrations.FindAsync(eventId, userId, ct) ?? throw Missing();
        if (registration.Status != RegistrationStatus.Confirmed)
            throw Conflict("Registration is already cancelled.");
        if (registration.Event.StartsAt <= DateTime.UtcNow)
            throw Invalid("Registration can only be cancelled before the event starts.");
        registration.Status = RegistrationStatus.Cancelled;
        events.MarkChanged(registration.Event);
        await transaction.CommitAsync(ct);
    }
    public async Task<PagedResult<RegistrationResponse>> MineAsync(int userId, RegistrationQuery query, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct);
        return await registrations.MineAsync(userId, query, ct);
    }
    public async Task<PagedResult<AttendeeResponse>> AttendeesAsync(int eventId, int organizerId, AttendeeQuery query, CancellationToken ct)
    {
        await attendeeValidator.ValidateAndThrowAsync(query, ct);
        var ev = await registrations.FindEventAsync(eventId, ct) ?? throw Missing();
        if (ev.Booking.UserId != organizerId)
            throw Forbidden();
        return await registrations.AttendeesAsync(eventId, query, ct);
    }
    public async Task<CheckInResponse> CheckInAsync(string code, int organizerId, CancellationToken ct)
    {
        if (code.Length != 10 || !code.StartsWith("EVT-", StringComparison.Ordinal) || !code[4..].All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9'))
            throw Missing();
        var eventId = await registrations.FindTicketEventIdAsync(code, ct) ?? throw Missing();
        await using var transaction = await BeginAsync(eventId, ct);
        var ticket = await registrations.FindTicketAsync(code, ct) ?? throw Missing();
        var registration = ticket.Registration;
        var ev = registration.Event;
        if (ev.Booking.UserId != organizerId)
            throw Forbidden();
        if (registration.Status != RegistrationStatus.Confirmed || ev.Status != EventStatus.Published || ev.Booking.Status != BookingStatus.Confirmed)
            throw Invalid("This registration or event is no longer active.");
        var now = DateTime.UtcNow;
        if (now < ev.StartsAt.AddHours(-2) || now >= ev.EndsAt)
            throw Invalid("Check-in is available from two hours before start until event end.");
        if (ticket.CheckedInAt.HasValue)
            throw Conflict("Ticket has already been checked in.");
        ticket.CheckedInAt = now;
        await transaction.CommitAsync(ct);
        return new(ticket.Code, eventId, ev.Title, registration.User.FullName, now);
    }
    private async Task<ISpaceBookingTransaction> BeginAsync(int eventId, CancellationToken ct)
        => await bookings.BeginAsync(await events.FindSpaceIdAsync(eventId, ct) ?? throw Missing(), ct);
    private async Task<string> NewCodeAsync(CancellationToken ct)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var characters = new char[6];
            for (var i = 0; i < characters.Length; i++)
                characters[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            var code = "EVT-" + new string(characters);
            if (!await registrations.CodeExistsAsync(code, ct))
                return code;
        }
        throw Conflict("Could not allocate a unique ticket code. Retry the request.");
    }
    private static BusinessException Missing() => new(FailureKind.NotFound, "Event, registration or ticket was not found.");
    private static BusinessException Forbidden() => new(FailureKind.Forbidden, "Only the event organizer can access attendees or check in tickets.");
    private static BusinessException Invalid(string message) => new(FailureKind.Validation, message);
    private static BusinessException Conflict(string message) => new(FailureKind.Conflict, message);
}
