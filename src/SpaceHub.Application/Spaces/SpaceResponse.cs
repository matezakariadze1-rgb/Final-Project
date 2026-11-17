namespace SpaceHub.Application.Spaces;

public sealed class SpaceResponse
{
    public int Id
    {
        get; set;
    }
    public int OwnerId
    {
        get; set;
    }
    public string OwnerName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Capacity
    {
        get; set;
    }
    public decimal HourlyRate
    {
        get; set;
    }
    public string Currency { get; set; } = string.Empty;
    public bool IsActive
    {
        get; set;
    }
    public int WorkingDays
    {
        get; set;
    }
    public byte[] RowVersion { get; set; } = [];
}
