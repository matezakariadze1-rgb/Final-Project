namespace SpaceHub.Application.Spaces;

public sealed record UpdateSpaceRequest(string Name, string Description, string Address, string City, int Capacity, decimal HourlyRate, byte[] RowVersion);
