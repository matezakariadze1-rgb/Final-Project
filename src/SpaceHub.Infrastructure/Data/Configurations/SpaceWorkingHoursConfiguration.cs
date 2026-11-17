using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data.Configurations;

public sealed class SpaceWorkingHoursConfiguration : IEntityTypeConfiguration<SpaceWorkingHours>
{
    public void Configure(EntityTypeBuilder<SpaceWorkingHours> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.SpaceId, x.DayOfWeek }).IsUnique();
        b.HasOne(x => x.Space).WithMany(x => x.WorkingHours).HasForeignKey(x => x.SpaceId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SpaceWorkingHours_Hours", "[ClosesAt] > [OpensAt]");
            t.HasCheckConstraint("CK_SpaceWorkingHours_Day", "[DayOfWeek] BETWEEN 0 AND 6");
        });
    }
}
