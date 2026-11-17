using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Domain.Entities;

public sealed class Ticket
{
    public int Id
    {
        get; set;
    }
    public int RegistrationId
    {
        get; set;
    }
    public string Code { get; set; } = string.Empty;
    public DateTime IssuedAt
    {
        get; set;
    }
    public DateTime? CheckedInAt
    {
        get; set;
    }
    public byte[] RowVersion { get; set; } = [];
    public Registration Registration { get; set; } = null!;
}
