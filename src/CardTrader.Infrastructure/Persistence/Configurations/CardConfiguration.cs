using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CardTrader.Infrastructure.Persistence.Configurations;

internal sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, guid => new CardId(guid));
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.SetName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Rarity).IsRequired().HasMaxLength(50);
        builder.Ignore(c => c.DomainEvents);
    }
}
