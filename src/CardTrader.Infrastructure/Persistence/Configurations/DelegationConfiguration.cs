using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CardTrader.Infrastructure.Persistence.Configurations;

internal sealed class DelegationConfiguration : IEntityTypeConfiguration<Delegation>
{
    public void Configure(EntityTypeBuilder<Delegation> builder)
    {
        builder.ToTable("delegations");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasConversion(id => id.Value, guid => new DelegationId(guid));
        builder.Property(d => d.DelegatorId)
            .HasConversion(id => id.Value, guid => new UserId(guid))
            .IsRequired();
        builder.Property(d => d.DelegateeId)
            .HasConversion(id => id.Value, guid => new UserId(guid))
            .IsRequired();
        builder.Property(d => d.IsActive).IsRequired();
        builder.Property(d => d.ExpiresAt); // nullable DateTimeOffset, no extra config needed
        builder.Ignore(d => d.DomainEvents);
    }
}
