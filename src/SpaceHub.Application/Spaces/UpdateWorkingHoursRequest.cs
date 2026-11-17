namespace SpaceHub.Application.Spaces;

public sealed record UpdateWorkingHoursRequest(List<WorkingDayRequest> Days, byte[] RowVersion);
