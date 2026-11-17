using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data.Configurations;

public sealed class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
        b.HasOne(x => x.Event).WithMany(x => x.Registrations).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.User).WithMany(x => x.Registrations).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Registration_Status", "[Status] IN (0,1)");
        });
    }
}
