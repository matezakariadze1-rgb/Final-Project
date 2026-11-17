namespace SpaceHub.Application.Auth;

public sealed record UserResponse(int Id, string Email, string FullName, IReadOnlyList<string> Roles);
