using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data.Configurations;

public sealed class BookingServiceConfiguration : IEntityTypeConfiguration<BookingService>
{
    public void Configure(EntityTypeBuilder<BookingService> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.NameSnapshot).HasMaxLength(120).IsRequired();
        b.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 2);
        b.Property(x => x.Subtotal).HasPrecision(18, 2);
        b.HasOne(x => x.Booking).WithMany(x => x.Services).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Service).WithMany(x => x.BookingServices).HasForeignKey(x => x.SpaceServiceId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_BookingService_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_BookingService_Amounts", "[UnitPriceSnapshot] >= 0 AND [Subtotal] >= 0");
            t.HasCheckConstraint("CK_BookingService_Included", "[IsIncludedInPackage] = 0 OR [Subtotal] = 0");
        });
    }
}
