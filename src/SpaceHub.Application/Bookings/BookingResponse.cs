using SpaceHub.Domain.Entities.Enums;
namespace SpaceHub.Application.Bookings;

public sealed class BookingResponse
{
    public int Id
    {
        get; set;
    }
    public int UserId
    {
        get; set;
    }
    public string CustomerName { get; set; } = string.Empty;
    public int SpaceId
    {
        get; set;
    }
    public string SpaceName { get; set; } = string.Empty;
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
    public BookingStatus Status
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
    public string? DecisionReason
    {
        get; set;
    }
    public DateTime CreatedAt
    {
        get; set;
    }
    public DateTime UpdatedAt
    {
        get; set;
    }
    public byte[] RowVersion { get; set; } = [];
    public List<BookingLineResponse> Services { get; set; } = [];
}
