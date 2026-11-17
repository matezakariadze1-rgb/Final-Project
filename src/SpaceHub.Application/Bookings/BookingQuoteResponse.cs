namespace SpaceHub.Application.Bookings;

public sealed class BookingQuoteResponse
{
    public int SpaceId
    {
        get; set;
    }
    public DateTime StartsAt
    {
        get; set;
    }
    public DateTime EndsAt
    {
        get; set;
    }
    public int AttendeeCount
    {
        get; set;
    }
    public decimal HourlyRateSnapshot
    {
        get; set;
    }
    public decimal SpaceSubtotal
    {
        get; set;
    }
    public int? PackageId
    {
        get; set;
    }
    public string? PackageNameSnapshot
    {
        get; set;
    }
    public decimal PackagePriceSnapshot
    {
        get; set;
    }
    public decimal ServicesSubtotal
    {
        get; set;
    }
    public decimal TotalAmount
    {
        get; set;
    }
    public string Currency { get; set; } = string.Empty;
    public List<BookingLineResponse> Services { get; set; } = [];
    public byte[] SpaceRowVersion { get; set; } = [];
}
