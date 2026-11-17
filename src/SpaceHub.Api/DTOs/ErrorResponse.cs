namespace SpaceHub.Api.DTOs;

public sealed record ErrorResponse(int Status, string Message, IReadOnlyDictionary<string, string[]> Errors)
{
    public static ErrorResponse Create(int status, string message) => new(status, message, new Dictionary<string, string[]>());
}
