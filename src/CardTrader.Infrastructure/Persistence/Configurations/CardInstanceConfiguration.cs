using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CardTrader.Infrastructure.Persistence.Configurations;

internal sealed class CardInstanceConfiguration : IEntityTypeConfiguration<CardInstance>
{
    public void Configure(EntityTypeBuilder<CardInstance> builder)
    {
        builder.ToTable("card_instances");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, guid => new CardInstanceId(guid));
        builder.Property(c => c.CardId)
            .HasConversion(id => id.Value, guid => new CardId(guid))
            .IsRequired();
        builder.Property(c => c.OwnerId)
            .HasConversion(id => id.Value, guid => new UserId(guid))
            .IsRequired();
        builder.Property(c => c.PrintNumber).IsRequired();
        builder.Property(c => c.RosterId)
            .HasConversion(
                v => v == null ? (Guid?)null : v.Value.Value,
                v => v == null ? (RosterId?)null : new RosterId(v.Value))
            .HasColumnName("roster_id")
            .IsRequired(false);
        builder.HasIndex(ci => new { ci.CardId, ci.PrintNumber }).IsUnique();
        builder.Ignore(c => c.DomainEvents);
    }
}
