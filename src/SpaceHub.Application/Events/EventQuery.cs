using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Events;

public sealed class EventQuery
{
    public string? Search
    {
        get; init;
    }
    public string? City
    {
        get; init;
    }
    public int? TagId
    {
        get; init;
    }
    public DateTimeOffset? From
    {
        get; init;
    }
    public DateTimeOffset? To
    {
        get; init;
    }
    public bool FreeOnly
    {
        get; init;
    }
    public EventStatus? Status
    {
        get; init;
    }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
