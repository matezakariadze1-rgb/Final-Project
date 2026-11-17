namespace SpaceHub.Application.Spaces;

public sealed record CreateSpaceRequest(string Name, string Description, string Address, string City, int Capacity, decimal HourlyRate);
