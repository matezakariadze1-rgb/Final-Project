using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Registrations;

public sealed class RegistrationQuery
{
    public bool UpcomingOnly
    {
        get; init;
    }
    public RegistrationStatus? Status
    {
        get; init;
    }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
