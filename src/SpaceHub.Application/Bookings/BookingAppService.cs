using AutoMapper;
using FluentValidation;
using SpaceHub.Application.Common;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Bookings;

public sealed class BookingAppService : IBookingService
{
    private readonly IBookingRepository repository;
    private readonly IMapper mapper;
    private readonly IValidator<BookingQuoteRequest> quoteValidator;
    private readonly IValidator<CreateBookingRequest> createValidator;
    private readonly IValidator<ConfirmBookingRequest> confirmValidator;
    private readonly IValidator<RejectBookingRequest> rejectValidator;
    private readonly IValidator<CancelBookingRequest> cancelValidator;
    private readonly IValidator<BookingQuery> queryValidator;

    public BookingAppService(IBookingRepository repository, IMapper mapper,
    IValidator<BookingQuoteRequest> quoteValidator, IValidator<CreateBookingRequest> createValidator,
    IValidator<ConfirmBookingRequest> confirmValidator, IValidator<RejectBookingRequest> rejectValidator,
    IValidator<CancelBookingRequest> cancelValidator, IValidator<BookingQuery> queryValidator)
    {
        this.repository = repository;
        this.mapper = mapper;
        this.quoteValidator = quoteValidator;
        this.createValidator = createValidator;
        this.confirmValidator = confirmValidator;
        this.rejectValidator = rejectValidator;
        this.cancelValidator = cancelValidator;
        this.queryValidator = queryValidator;
    }

    public async Task<AvailabilityResponse> AvailabilityAsync(int spaceId, AvailabilityQuery query, CancellationToken ct)
    {
        var request = new BookingQuoteRequest(spaceId, query.StartsAt, query.EndsAt, query.AttendeeCount, null, []);
        await quoteValidator.ValidateAndThrowAsync(request, ct);
        try
        {
            await QuoteAsync(request, ct);
            return new(spaceId, true, null);
        }
        catch (BusinessException ex) when (ex.Kind is FailureKind.Validation or FailureKind.Conflict)
        {
            return new(spaceId, false, ex.Message);
        }
    }
    public async Task<BookingQuoteResponse> QuoteAsync(BookingQuoteRequest request, CancellationToken ct)
    {
        await quoteValidator.ValidateAndThrowAsync(request, ct);
        var space = await repository.LoadSpaceAsync(request.SpaceId, ct) ?? throw Missing();
        var priced = BookingRules.Price(space, request);
        await CheckAvailabilityAsync(priced, ct);
        return mapper.Map<BookingQuoteResponse>(priced);
    }
    public async Task<BookingResponse> CreateAsync(CreateBookingRequest request, int userId, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        await using var transaction = await repository.BeginAsync(request.SpaceId, ct);
        CheckVersion(transaction.Space.RowVersion, request.SpaceRowVersion);
        var booking = BookingRules.Price(transaction.Space, new(request.SpaceId, request.StartsAt, request.EndsAt, request.AttendeeCount, request.PackageId, request.AdditionalServices));
        await CheckAvailabilityAsync(booking, ct);
        booking.User = await repository.FindUserAsync(userId, ct) ?? throw new BusinessException(FailureKind.Unauthorized, "Account is not available.");
        booking.UserId = userId;
        booking.CreatedAt = booking.UpdatedAt = DateTime.UtcNow;
        repository.Add(booking);
        await transaction.CommitAsync(ct);
        return mapper.Map<BookingResponse>(booking);
    }
    public async Task<BookingResponse> GetAsync(int id, int userId, CancellationToken ct)
    {
        var booking = await repository.FindAsync(id, ct) ?? throw Missing();
        if (booking.UserId != userId && booking.Space.OwnerId != userId)
            throw Forbidden();
        return mapper.Map<BookingResponse>(booking);
    }
    public async Task<PagedResult<BookingResponse>> ListAsync(int userId, bool received, BookingQuery query, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct);
        var result = await repository.ListAsync(userId, received, query, ct);
        return new(mapper.Map<List<BookingResponse>>(result.Items), result.TotalCount, result.Page, result.PageSize);
    }
    public async Task<BookingResponse> ConfirmAsync(int id, ConfirmBookingRequest request, int userId, CancellationToken ct)
    {
        await confirmValidator.ValidateAndThrowAsync(request, ct);
        await using var transaction = await BeginForBookingAsync(id, ct);
        var booking = await repository.FindAsync(id, ct) ?? throw Missing();
        if (transaction.Space.OwnerId != userId)
            throw Forbidden();
        CheckVersion(booking.RowVersion, request.RowVersion);
        if (booking.Status != BookingStatus.Pending)
            throw Conflict("Only pending bookings can be confirmed.");
        BookingRules.ValidatePeriod(transaction.Space, booking.StartsAt, booking.EndsAt, booking.AttendeeCount);
        bool packageIsUnavailable = booking.PackageId.HasValue
            && !transaction.Space.Packages.Any(x => x.Id == booking.PackageId && x.IsActive);
        bool serviceIsUnavailable = booking.Services.Any(line =>
            !transaction.Space.Services.Any(x => x.Id == line.SpaceServiceId && x.IsActive));
        if (packageIsUnavailable || serviceIsUnavailable)
            throw BookingRules.Invalid("A selected package or service is no longer active.");
        await CheckAvailabilityAsync(booking, ct);
        booking.Status = BookingStatus.Confirmed;
        booking.UpdatedAt = DateTime.UtcNow;
        foreach (var other in await repository.OverlappingAsync(booking.SpaceId, booking.StartsAt, booking.EndsAt, BookingStatus.Pending, ct))
        {
            if (other.Id == booking.Id)
                continue;
            other.Status = BookingStatus.Rejected;
            other.DecisionReason = "Another booking was confirmed for this time.";
            other.UpdatedAt = DateTime.UtcNow;
        }
        await transaction.CommitAsync(ct);
        return mapper.Map<BookingResponse>(booking);
    }
    public async Task<BookingResponse> RejectAsync(int id, RejectBookingRequest request, int userId, CancellationToken ct)
    {
        await rejectValidator.ValidateAndThrowAsync(request, ct);
        await using var transaction = await BeginForBookingAsync(id, ct);
        var booking = await repository.FindAsync(id, ct) ?? throw Missing();
        if (transaction.Space.OwnerId != userId)
            throw Forbidden();
        CheckVersion(booking.RowVersion, request.RowVersion);
        if (booking.Status != BookingStatus.Pending)
            throw Conflict("Only pending bookings can be rejected.");
        booking.Status = BookingStatus.Rejected;
        booking.DecisionReason = request.Reason.Trim();
        booking.UpdatedAt = DateTime.UtcNow;
        await transaction.CommitAsync(ct);
        return mapper.Map<BookingResponse>(booking);
    }
    public async Task<BookingResponse> CancelAsync(int id, CancelBookingRequest request, int userId, CancellationToken ct)
    {
        await cancelValidator.ValidateAndThrowAsync(request, ct);
        await using var transaction = await BeginForBookingAsync(id, ct);
        var booking = await repository.FindAsync(id, ct) ?? throw Missing();
        var owner = transaction.Space.OwnerId == userId;
        if (!owner && booking.UserId != userId)
            throw Forbidden();
        CheckVersion(booking.RowVersion, request.RowVersion);
        var now = DateTime.UtcNow;
        bool statusAllowsCancellation = booking.Status == BookingStatus.Pending
            || booking.Status == BookingStatus.Confirmed;
        bool bookingHasEnded = booking.EndsAt <= now;
        bool pendingBookingHasStarted = booking.Status == BookingStatus.Pending && booking.StartsAt <= now;
        if (!statusAllowsCancellation || bookingHasEnded || pendingBookingHasStarted)
            throw Conflict("This booking can no longer be cancelled.");
        if (owner && string.IsNullOrWhiteSpace(request.Reason))
            throw BookingRules.Invalid("Owner cancellation requires a reason.");
        bool customerDeadlinePassed;
        if (booking.Status == BookingStatus.Pending)
        {
            customerDeadlinePassed = booking.StartsAt <= now;
        }
        else
        {
            customerDeadlinePassed = now > booking.StartsAt.AddHours(-24);
        }
        if (!owner && customerDeadlinePassed)
            throw BookingRules.Invalid("Pending bookings can be cancelled before start; confirmed bookings require at least 24 hours notice.");
        booking.Status = BookingStatus.Cancelled;
        booking.DecisionReason = request.Reason?.Trim();
        booking.UpdatedAt = now;
        if (booking.Event is not null)
        {
            booking.Event.Status = EventStatus.Cancelled;
            foreach (var registration in booking.Event.Registrations.Where(x => x.Status == RegistrationStatus.Confirmed))
                registration.Status = RegistrationStatus.Cancelled;
        }
        await transaction.CommitAsync(ct);
        return mapper.Map<BookingResponse>(booking);
    }
    public async Task RefreshDueAsync(CancellationToken ct)
    {
        foreach (var id in await repository.DueSpaceIdsAsync(ct))
        {
            await using var transaction = await repository.BeginAsync(id, ct);
            var due = await repository.DueBookingsAsync(id, ct);
            if (due.Count == 0)
                continue;
            foreach (var booking in due)
            {
                if (booking.Status == BookingStatus.Pending)
                {
                    booking.Status = BookingStatus.Rejected;
                    booking.DecisionReason = "The booking start time passed without confirmation.";
                }
                else
                {
                    booking.Status = BookingStatus.Completed;
                    if (booking.Event is { Status: EventStatus.Published })
                        booking.Event.Status = EventStatus.Completed;
                }
                booking.UpdatedAt = DateTime.UtcNow;
            }
            await transaction.CommitAsync(ct);
        }
    }
    private async Task<ISpaceBookingTransaction> BeginForBookingAsync(int id, CancellationToken ct)
    {
        var spaceId = await repository.FindSpaceIdAsync(id, ct);
        if (spaceId == null)
        {
            throw Missing();
        }
        return await repository.BeginAsync(spaceId.Value, ct);
    }
    private async Task CheckAvailabilityAsync(Booking booking, CancellationToken ct)
    {
        if ((await repository.OverlappingAsync(booking.SpaceId, booking.StartsAt, booking.EndsAt, BookingStatus.Confirmed, ct)).Any(x => x.Id != booking.Id))
            throw Conflict("Space is already booked during this period.");
    }
    private static void CheckVersion(byte[] current, byte[] expected)
    {
        if (!current.SequenceEqual(expected))
            throw Conflict("The record changed. Reload it and retry.");
    }
    private static BusinessException Missing() => new(FailureKind.NotFound, "Booking or space was not found.");
    private static BusinessException Forbidden() => new(FailureKind.Forbidden, "You cannot access or manage this booking.");
    private static BusinessException Conflict(string message) => new(FailureKind.Conflict, message);
}
