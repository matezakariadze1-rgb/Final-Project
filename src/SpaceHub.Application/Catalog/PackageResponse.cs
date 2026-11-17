namespace SpaceHub.Application.Catalog;

public sealed class PackageResponse
{
    public int Id
    {
        get; set;
    }
    public int SpaceId
    {
        get; set;
    }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price
    {
        get; set;
    }
    public string Currency { get; set; } = string.Empty;
    public bool IsActive
    {
        get; set;
    }
    public List<PackageItemResponse> Items { get; set; } = [];
    public byte[] SpaceRowVersion { get; set; } = [];
}
