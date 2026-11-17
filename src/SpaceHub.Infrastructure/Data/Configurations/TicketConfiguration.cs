using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceHub.Domain.Entities;

namespace SpaceHub.Infrastructure.Data.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(64).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.RegistrationId).IsUnique();
        b.HasIndex(x => x.Code).IsUnique();
        b.HasOne(x => x.Registration).WithOne(x => x.Ticket).HasForeignKey<Ticket>(x => x.RegistrationId).OnDelete(DeleteBehavior.Restrict);
    }
}
