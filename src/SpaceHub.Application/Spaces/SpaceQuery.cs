namespace SpaceHub.Application.Spaces;

public sealed class SpaceQuery
{
    public string? Search
    {
        get; init;
    }
    public string? City
    {
        get; init;
    }
    public int? MinCapacity
    {
        get; init;
    }
    public decimal? MaxHourlyRate
    {
        get; init;
    }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
