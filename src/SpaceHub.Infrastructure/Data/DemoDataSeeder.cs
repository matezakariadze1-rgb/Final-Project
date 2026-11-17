using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SpaceHub.Application.Auth;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Infrastructure.Data;

/// <summary>Explicit, atomic demo import. Existing reserved accounts are never changed.</summary>
public sealed class DemoDataSeeder
{
    private readonly AppDbContext db;
    private readonly IPasswordService passwords;

    // დამოკიდებულებები კონსტრუქტორით გადმოგვეცემა.
    public DemoDataSeeder(AppDbContext db, IPasswordService passwords)
    {
        this.db = db;
        this.passwords = passwords;
    }

    public const string Password = "SpaceHubDemo!2026";
    public static readonly string[] Emails =
        ["owner.demo@spacehub.example", "organizer.demo@spacehub.example", "attendee.demo@spacehub.example"];

    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Concurrent imports serialize; all records are committed together or rolled back.
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource=N'SpaceHub.DemoSeed',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
            IF @result < 0 THROW 51000, 'Could not lock demo import.', 1;
            """, cancellationToken);
        if (await db.UserRecords.AnyAsync(x => Emails.Contains(x.Email), cancellationToken))
            return false;

        var now = DateTime.UtcNow;
        // 10:00 in Georgia, two days after import. Persisted dates remain UTC.
        var firstStart = DateTime.SpecifyKind(now.AddHours(4).Date.AddDays(2).AddHours(6), DateTimeKind.Utc);
        var roles = await db.RoleRecords.ToDictionaryAsync(x => x.Name, cancellationToken);
        User Account(int index, string name, params string[] roleNames)
        {
            var user = new User { Email = Emails[index], FullName = name, CreatedAt = now };
            user.PasswordHash = passwords.Hash(user, Password);
            foreach (var role in roleNames)
                user.UserRoles.Add(new UserRole { Role = roles[role] });
            return user;
        }
        var owner = Account(0, "Demo Space Owner", "Attendee", "Owner");
        var organizer = Account(1, "Demo Event Organizer", "Attendee", "Organizer");
        var attendee = Account(2, "Demo Attendee", "Attendee");
        organizer.OrganizerProfile = new OrganizerProfile
        {
            CompanyName = "SpaceHub Demo Events",
            Phone = "+995555010101",
            Website = "https://example.com",
            Description = "Local demonstration organizer."
        };

        Space Venue(string name, string city, int capacity, decimal rate) => new()
        {
            Owner = owner,
            Name = name,
            City = city,
            Address = "Demo address, 1",
            Description = "Demonstration venue for bookings and events.",
            Capacity = capacity,
            HourlyRate = rate,
            WorkingHours = Enum.GetValues<DayOfWeek>().Select(day => new SpaceWorkingHours
            {
                DayOfWeek = day,
                OpensAt = new TimeOnly(8, 0),
                ClosesAt = new TimeOnly(23, 0)
            }).ToList()
        };
        var tbilisi = Venue("Demo Tbilisi Conference Hall", "Tbilisi", 60, 100);
        var batumi = Venue("Demo Batumi Workshop Studio", "Batumi", 30, 70);
        var projector = new SpaceService { Space = tbilisi, Name = "Projector", UnitPrice = 40, PricingType = ServicePricingType.PerUnit };
        var coffee = new SpaceService { Space = tbilisi, Name = "Coffee break", UnitPrice = 10, PricingType = ServicePricingType.PerPerson };
        var microphone = new SpaceService { Space = batumi, Name = "Microphone", UnitPrice = 25, PricingType = ServicePricingType.PerUnit };
        var package = new Package
        {
            Space = tbilisi,
            Name = "Demo Conference Package",
            Description = "Projector and coffee for 20 guests.",
            Price = 200,
            Items = [new PackageItem { Service = projector, Quantity = 1 }, new PackageItem { Service = coffee, Quantity = 20 }]
        };
        Booking Reserve(Space space, int day, BookingStatus status) => new()
        {
            User = organizer,
            Space = space,
            StartsAt = firstStart.AddDays(day),
            EndsAt = firstStart.AddDays(day).AddHours(3),
            AttendeeCount = 25,
            Status = status,
            HourlyRateSnapshot = space.HourlyRate,
            SpaceSubtotal = space.HourlyRate * 3,
            TotalAmount = space.HourlyRate * 3,
            CreatedAt = now,
            UpdatedAt = now
        };
        var publicBooking = Reserve(tbilisi, 0, BookingStatus.Confirmed);
        publicBooking.Package = package;
        publicBooking.PackageNameSnapshot = package.Name;
        publicBooking.PackagePriceSnapshot = package.Price;
        publicBooking.ServicesSubtotal = 50;
        publicBooking.TotalAmount += 250;
        publicBooking.Services =
        [
            new BookingService { Service = projector, NameSnapshot = projector.Name, PricingTypeSnapshot = projector.PricingType, Quantity = 1, UnitPriceSnapshot = 40, IsIncludedInPackage = true },
            new BookingService { Service = coffee, NameSnapshot = coffee.Name, PricingTypeSnapshot = coffee.PricingType, Quantity = 20, UnitPriceSnapshot = 10, IsIncludedInPackage = true },
            new BookingService { Service = coffee, NameSnapshot = coffee.Name, PricingTypeSnapshot = coffee.PricingType, Quantity = 5, UnitPriceSnapshot = 10, Subtotal = 50 }
        ];
        var privateBooking = Reserve(batumi, 1, BookingStatus.Confirmed);
        var draftBooking = Reserve(tbilisi, 2, BookingStatus.Confirmed);
        var pendingBooking = Reserve(batumi, 3, BookingStatus.Pending);
        var tag = await db.TagRecords.FirstAsync(x => x.Name == "IT", cancellationToken);
        Event Meeting(Booking booking, string title, EventStatus status, EventVisibility visibility) => new()
        {
            Booking = booking,
            Title = title,
            Description = "Demonstration event: explore registration, tickets and organizer tools.",
            StartsAt = booking.StartsAt.AddMinutes(30),
            EndsAt = booking.EndsAt.AddMinutes(-30),
            MaxAttendees = 25,
            TicketPrice = 0,
            Status = status,
            Visibility = visibility,
            CreatedAt = now,
            AccessCode = visibility == EventVisibility.Private ? Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) : null,
            EventTags = [new EventTag { Tag = tag }]
        };
        var publicEvent = Meeting(publicBooking, "Demo .NET Community Meetup", EventStatus.Published, EventVisibility.Public);
        var privateEvent = Meeting(privateBooking, "Demo Private Team Workshop", EventStatus.Published, EventVisibility.Private);
        var draftEvent = Meeting(draftBooking, "Demo Upcoming Workshop Draft", EventStatus.Draft, EventVisibility.Public);
        draftEvent.TicketPrice = 20;

        // Share the allocator lock with the live registration service.
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource=N'SpaceHub.TicketCodes',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
            IF @result < 0 THROW 51000, 'Could not lock ticket allocation.', 1;
            """, cancellationToken);
        var allocated = new HashSet<string>();
        async Task Register(Event meeting, User user)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string code;
            do
            {
                code = "EVT-" + new string(Enumerable.Range(0, 6).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());
            }
            while (allocated.Contains(code) || await db.TicketRecords.AnyAsync(x => x.Code == code, cancellationToken));
            allocated.Add(code);
            meeting.Registrations.Add(new Registration
            {
                User = user,
                RegisteredAt = now,
                Status = RegistrationStatus.Confirmed,
                Ticket = new Ticket { Code = code, IssuedAt = now }
            });
        }
        await Register(publicEvent, attendee);
        await Register(publicEvent, owner);
        await Register(privateEvent, attendee);
        db.AddRange(owner, organizer, attendee, tbilisi, batumi, projector, coffee, microphone,
            package, publicBooking, privateBooking, draftBooking, pendingBooking, publicEvent, privateEvent, draftEvent);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
