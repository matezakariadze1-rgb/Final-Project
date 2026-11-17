namespace SpaceHub.Application.Spaces;

public sealed record WorkingHoursResponse(int SpaceId, string TimeZone, IReadOnlyList<WorkingDayResponse> Days, byte[] RowVersion);
