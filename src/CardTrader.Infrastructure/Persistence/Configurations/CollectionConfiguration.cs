using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CardTrader.Infrastructure.Persistence.Configurations;

internal sealed class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.ToTable("collections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, guid => new CollectionId(guid));
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.OwnerId)
            .HasConversion(id => id.Value, guid => new UserId(guid))
            .IsRequired();
        builder.Ignore(c => c.DomainEvents);
    }
}
