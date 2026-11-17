using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class PackageItem
{
    public int PackageId
    {
        get; set;
    }
    public int SpaceServiceId
    {
        get; set;
    }
    public int Quantity
    {
        get; set;
    }
    public Package Package { get; set; } = null!;
    public SpaceService Service { get; set; } = null!;
}
