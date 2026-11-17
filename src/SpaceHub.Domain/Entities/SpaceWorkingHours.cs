using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class SpaceWorkingHours
{
    public int Id
    {
        get; set;
    }
    public int SpaceId
    {
        get; set;
    }
    public DayOfWeek DayOfWeek
    {
        get; set;
    }
    public TimeOnly OpensAt
    {
        get; set;
    }
    public TimeOnly ClosesAt
    {
        get; set;
    }
    public Space Space { get; set; } = null!;
}
