using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(120).IsRequired();
        b.Property(x => x.Description).HasMaxLength(3000).IsRequired();
        b.Property(x => x.TicketPrice).HasPrecision(18, 2);
        b.Property(x => x.AccessCode).HasMaxLength(64);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.BookingId).IsUnique();
        b.HasIndex(x => x.AccessCode).IsUnique();
        b.HasOne(x => x.Booking).WithOne(x => x.Event).HasForeignKey<Event>(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Event_Period", "[EndsAt] > [StartsAt]");
            t.HasCheckConstraint("CK_Event_Capacity", "[MaxAttendees] > 0");
            t.HasCheckConstraint("CK_Event_Price", "[TicketPrice] >= 0");
            t.HasCheckConstraint("CK_Event_Status", "[Status] BETWEEN 0 AND 3");
            t.HasCheckConstraint("CK_Event_Visibility", "[Visibility] IN (0,1)");
            t.HasCheckConstraint("CK_Event_PrivateCode", "[Visibility] = 0 OR [AccessCode] IS NOT NULL");
        });
    }
}
