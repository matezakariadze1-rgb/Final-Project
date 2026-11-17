using AutoMapper;
using FluentValidation;
using SpaceHub.Application.Common;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Entities;
namespace SpaceHub.Application.Spaces;

public sealed class SpaceService : ISpaceService
{
    private readonly ISpaceRepository spaces;
    private readonly IMapper mapper;
    private readonly IValidator<CreateSpaceRequest> createValidator;
    private readonly IValidator<UpdateSpaceRequest> updateValidator;
    private readonly IValidator<UpdateSpaceStatusRequest> statusValidator;
    private readonly IValidator<SpaceQuery> queryValidator;
    private readonly IValidator<UpdateWorkingHoursRequest> hoursValidator;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public SpaceService(ISpaceRepository spaces, IMapper mapper,
    IValidator<CreateSpaceRequest> createValidator, IValidator<UpdateSpaceRequest> updateValidator,
    IValidator<UpdateSpaceStatusRequest> statusValidator, IValidator<SpaceQuery> queryValidator,
    IValidator<UpdateWorkingHoursRequest> hoursValidator)
    {
        this.spaces = spaces;
        this.mapper = mapper;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
        this.statusValidator = statusValidator;
        this.queryValidator = queryValidator;
        this.hoursValidator = hoursValidator;
    }

    public async Task<PagedResult<SpaceResponse>> SearchAsync(SpaceQuery query, int? ownerId, CancellationToken ct)
    {
        await queryValidator.ValidateAndThrowAsync(query, ct);
        var result = await spaces.SearchAsync(query, ownerId, ct);
        return new(mapper.Map<List<SpaceResponse>>(result.Items), result.TotalCount, result.Page, result.PageSize);
    }
    public async Task<SpaceResponse> GetAsync(int id, int userId, CancellationToken ct)
        => mapper.Map<SpaceResponse>(await VisibleSpaceAsync(id, userId, ct));
    public async Task<SpaceResponse> CreateAsync(CreateSpaceRequest request, int userId, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        var space = new Space
        {
            OwnerId = userId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Capacity = request.Capacity,
            HourlyRate = request.HourlyRate
        };
        await spaces.AddAsync(space, ct);
        return mapper.Map<SpaceResponse>(space);
    }
    public async Task<SpaceResponse> UpdateAsync(int id, UpdateSpaceRequest request, int userId, CancellationToken ct)
    {
        var space = await OwnedSpaceAsync(id, userId, ct);
        await updateValidator.ValidateAndThrowAsync(request, ct);
        if (request.Capacity < space.Capacity && (await spaces.GetFutureConfirmedBookingsAsync(id, ct)).Any(x => x.AttendeeCount > request.Capacity))
            throw new BusinessException(FailureKind.Validation, "Capacity cannot be below the size of an upcoming confirmed booking.");
        space.Name = request.Name.Trim();
        space.Description = request.Description.Trim();
        space.Address = request.Address.Trim();
        space.City = request.City.Trim();
        space.Capacity = request.Capacity;
        space.HourlyRate = request.HourlyRate;
        await spaces.SaveAsync(space, request.RowVersion, ct);
        return mapper.Map<SpaceResponse>(space);
    }
    public async Task<SpaceResponse> SetStatusAsync(int id, UpdateSpaceStatusRequest request, int userId, CancellationToken ct)
    {
        var space = await OwnedSpaceAsync(id, userId, ct);
        await statusValidator.ValidateAndThrowAsync(request, ct);
        space.IsActive = request.IsActive!.Value;
        await spaces.SaveAsync(space, request.RowVersion, ct);
        return mapper.Map<SpaceResponse>(space);
    }
    public async Task<WorkingHoursResponse> GetWorkingHoursAsync(int id, int userId, CancellationToken ct)
        => Hours(await VisibleSpaceAsync(id, userId, ct));
    public async Task<WorkingHoursResponse> SetWorkingHoursAsync(int id, UpdateWorkingHoursRequest request, int userId, CancellationToken ct)
    {
        var space = await OwnedSpaceAsync(id, userId, ct);
        await hoursValidator.ValidateAndThrowAsync(request, ct);
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tbilisi");
        foreach (var booking in await spaces.GetFutureConfirmedBookingsAsync(id, ct))
        {
            var start = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(booking.StartsAt, DateTimeKind.Utc), zone);
            var end = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(booking.EndsAt, DateTimeKind.Utc), zone);
            var day = request.Days.SingleOrDefault(x => x.DayOfWeek == start.DayOfWeek);
            if (day is null || start.Date != end.Date || TimeOnly.FromDateTime(start) < day.OpensAt || TimeOnly.FromDateTime(end) > day.ClosesAt)
                throw new BusinessException(FailureKind.Validation, "Working hours must cover all upcoming confirmed bookings.");
        }
        foreach (var existing in space.WorkingHours.ToList())
        {
            var replacement = request.Days.SingleOrDefault(x => x.DayOfWeek == existing.DayOfWeek);
            if (replacement is null)
            {
                spaces.RemoveWorkingDay(existing);
                space.WorkingHours.Remove(existing);
            }
            else
            {
                existing.OpensAt = replacement.OpensAt;
                existing.ClosesAt = replacement.ClosesAt;
            }
        }
        foreach (var day in request.Days.Where(x => space.WorkingHours.All(h => h.DayOfWeek != x.DayOfWeek)))
            space.WorkingHours.Add(new SpaceWorkingHours { Space = space, DayOfWeek = day.DayOfWeek, OpensAt = day.OpensAt, ClosesAt = day.ClosesAt });
        await spaces.SaveAsync(space, request.RowVersion, ct);
        return Hours(space);
    }
    private WorkingHoursResponse Hours(Space space) => new(space.Id, "Asia/Tbilisi",
        mapper.Map<List<WorkingDayResponse>>(space.WorkingHours.OrderBy(x => x.DayOfWeek)), space.RowVersion);
    private async Task<Space> VisibleSpaceAsync(int id, int userId, CancellationToken ct)
    {
        var space = await spaces.FindAsync(id, ct);
        if (space is null || !space.IsActive && space.OwnerId != userId)
            throw new BusinessException(FailureKind.NotFound, "Space was not found.");
        return space;
    }
    private async Task<Space> OwnedSpaceAsync(int id, int userId, CancellationToken ct)
    {
        var space = await spaces.FindAsync(id, ct) ?? throw new BusinessException(FailureKind.NotFound, "Space was not found.");
        if (space.OwnerId != userId)
            throw new BusinessException(FailureKind.Forbidden, "Only the owner can change this space.");
        return space;
    }
}
