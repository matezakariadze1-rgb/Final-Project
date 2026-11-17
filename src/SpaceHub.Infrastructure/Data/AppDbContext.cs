using Microsoft.EntityFrameworkCore;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> UserRecords => Set<User>();
    public DbSet<Role> RoleRecords => Set<Role>();
    public DbSet<UserRole> UserRoleRecords => Set<UserRole>();
    public DbSet<OrganizerProfile> OrganizerProfileRecords => Set<OrganizerProfile>();
    public DbSet<Space> SpaceRecords => Set<Space>();
    public DbSet<SpaceWorkingHours> SpaceWorkingHoursRecords => Set<SpaceWorkingHours>();
    public DbSet<SpaceService> SpaceServiceRecords => Set<SpaceService>();
    public DbSet<Package> PackageRecords => Set<Package>();
    public DbSet<PackageItem> PackageItemRecords => Set<PackageItem>();
    public DbSet<Booking> BookingRecords => Set<Booking>();
    public DbSet<BookingService> BookingServiceRecords => Set<BookingService>();
    public DbSet<Event> EventRecords => Set<Event>();
    public DbSet<Tag> TagRecords => Set<Tag>();
    public DbSet<EventTag> EventTagRecords => Set<EventTag>();
    public DbSet<Registration> RegistrationRecords => Set<Registration>();
    public DbSet<Ticket> TicketRecords => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
