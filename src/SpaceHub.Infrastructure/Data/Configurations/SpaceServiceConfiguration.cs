using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data.Configurations;

public sealed class SpaceServiceConfiguration : IEntityTypeConfiguration<SpaceService>
{
    public void Configure(EntityTypeBuilder<SpaceService> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.HasOne(x => x.Space).WithMany(x => x.Services).HasForeignKey(x => x.SpaceId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SpaceService_Price", "[UnitPrice] >= 0");
            t.HasCheckConstraint("CK_SpaceService_Pricing", "[PricingType] IN (0, 1)");
        });
    }
}
