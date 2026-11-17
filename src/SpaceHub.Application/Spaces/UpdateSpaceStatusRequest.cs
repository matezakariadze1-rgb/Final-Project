namespace SpaceHub.Application.Spaces;

public sealed record UpdateSpaceStatusRequest(bool? IsActive, byte[] RowVersion);
