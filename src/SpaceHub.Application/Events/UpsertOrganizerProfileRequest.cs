namespace SpaceHub.Application.Events;

public sealed record UpsertOrganizerProfileRequest(string CompanyName, string Phone, string? Website, string? Description);
