using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data.Configurations;

public sealed class PackageItemConfiguration : IEntityTypeConfiguration<PackageItem>
{
    public void Configure(EntityTypeBuilder<PackageItem> b)
    {
        b.HasKey(x => new { x.PackageId, x.SpaceServiceId });
        b.HasOne(x => x.Package).WithMany(x => x.Items).HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Service).WithMany(x => x.PackageItems).HasForeignKey(x => x.SpaceServiceId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_PackageItem_Quantity", "[Quantity] > 0");
        });
    }
}
