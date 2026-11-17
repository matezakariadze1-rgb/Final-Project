namespace SpaceHub.Application.Events;

public sealed record OrganizerProfileResponse(int Id, int UserId, string CompanyName, string Phone, string? Website, string? Description, bool IsVerified);
