using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
        b.HasData(new Tag { Id = 1, Name = "IT" }, new Tag { Id = 2, Name = "Music" }, new Tag { Id = 3, Name = "Workshop" });
    }
}
