using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data.Configurations;

public sealed class SpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Description).HasMaxLength(3000).IsRequired();
        b.Property(x => x.Address).HasMaxLength(200).IsRequired();
        b.Property(x => x.City).HasMaxLength(60).IsRequired();
        b.Property(x => x.HourlyRate).HasPrecision(18, 2);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne(x => x.Owner).WithMany(x => x.Spaces).HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Space_Capacity", "[Capacity] > 0");
            t.HasCheckConstraint("CK_Space_Rate", "[HourlyRate] >= 0");
        });
    }
}
