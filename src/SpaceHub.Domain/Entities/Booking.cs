using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class Booking
{
    public int Id
    {
        get; set;
    }
    public int UserId
    {
        get; set;
    }
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
    public string Currency { get; set; } = "GEL";
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
    public User User { get; set; } = null!;
    public Space Space { get; set; } = null!;
    public Package? Package
    {
        get; set;
    }
    public ICollection<BookingService> Services { get; set; } = new List<BookingService>();
    public Event? Event
    {
        get; set;
    }
}
