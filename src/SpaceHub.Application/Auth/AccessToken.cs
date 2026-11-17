namespace SpaceHub.Application.Auth;

public sealed record AccessToken(string Value, DateTime ExpiresAt);
