using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data.Configurations;

public sealed class EventTagConfiguration : IEntityTypeConfiguration<EventTag>
{
    public void Configure(EntityTypeBuilder<EventTag> b)
    {
        b.HasKey(x => new { x.EventId, x.TagId });
        b.HasOne(x => x.Event).WithMany(x => x.EventTags).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Tag).WithMany(x => x.EventTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
    }
}
