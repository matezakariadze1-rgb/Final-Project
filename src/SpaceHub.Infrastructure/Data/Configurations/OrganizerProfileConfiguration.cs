using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data.Configurations;

public sealed class OrganizerProfileConfiguration : IEntityTypeConfiguration<OrganizerProfile>
{
    public void Configure(EntityTypeBuilder<OrganizerProfile> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.CompanyName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        b.Property(x => x.Website).HasMaxLength(2048);
        b.Property(x => x.Description).HasMaxLength(3000);
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasOne(x => x.User).WithOne(x => x.OrganizerProfile).HasForeignKey<OrganizerProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
