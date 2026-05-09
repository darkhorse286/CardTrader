using CardTrader.Domain.Entities;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CardTrader.Infrastructure.Persistence.Configurations;

internal sealed class RosterConfiguration : IEntityTypeConfiguration<Roster>
{
    public void Configure(EntityTypeBuilder<Roster> builder)
    {
        builder.ToTable("rosters");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, guid => new RosterId(guid));
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.OwnerId)
            .HasConversion(id => id.Value, guid => new UserId(guid))
            .IsRequired();
        builder.Ignore(c => c.DomainEvents);
    }
}
