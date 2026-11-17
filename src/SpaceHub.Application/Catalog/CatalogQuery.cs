namespace SpaceHub.Application.Catalog;

public sealed class CatalogQuery
{
    public string? Search
    {
        get; init;
    }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
