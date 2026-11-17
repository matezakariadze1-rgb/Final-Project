using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.HourlyRateSnapshot).HasPrecision(18, 2);
        b.Property(x => x.SpaceSubtotal).HasPrecision(18, 2);
        b.Property(x => x.PackageNameSnapshot).HasMaxLength(120);
        b.Property(x => x.PackagePriceSnapshot).HasPrecision(18, 2);
        b.Property(x => x.ServicesSubtotal).HasPrecision(18, 2);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.DecisionReason).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.SpaceId, x.Status, x.StartsAt, x.EndsAt });
        b.HasOne(x => x.User).WithMany(x => x.Bookings).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Space).WithMany(x => x.Bookings).HasForeignKey(x => x.SpaceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Package).WithMany(x => x.Bookings).HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Booking_Period", "[EndsAt] > [StartsAt]");
            t.HasCheckConstraint("CK_Booking_Attendees", "[AttendeeCount] > 0");
            t.HasCheckConstraint("CK_Booking_Amounts", "[HourlyRateSnapshot] >= 0 AND [SpaceSubtotal] >= 0 AND [PackagePriceSnapshot] >= 0 AND [ServicesSubtotal] >= 0");
            t.HasCheckConstraint("CK_Booking_Total", "[TotalAmount] = [SpaceSubtotal] + [PackagePriceSnapshot] + [ServicesSubtotal]");
            t.HasCheckConstraint("CK_Booking_Currency", "[Currency] = 'GEL'");
            t.HasCheckConstraint("CK_Booking_Status", "[Status] BETWEEN 0 AND 4");
        });
    }
}
