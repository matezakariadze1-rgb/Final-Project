using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Registrations;

public sealed class AttendeeQuery
{
    public RegistrationStatus? Status
    {
        get; init;
    }
    public string? Search
    {
        get; init;
    }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
