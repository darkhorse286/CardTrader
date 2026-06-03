using CardTrader.Domain.Entities;
using CardTrader.Domain.Enums;
using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CardTrader.Infrastructure.Persistence.Configurations;

internal sealed class TradeProposalConfiguration : IEntityTypeConfiguration<TradeProposal>
{
    public void Configure(EntityTypeBuilder<TradeProposal> builder)
    {
        builder.ToTable("trade_proposals");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, guid => new TradeProposalId(guid));
        builder.Property(p => p.InitiatorId)
            .HasConversion(id => id.Value, guid => new UserId(guid))
            .IsRequired();
        builder.Property(p => p.RecipientId)
            .HasConversion(id => id.Value, guid => new UserId(guid))
            .IsRequired();
        builder.Property(p => p.InitiatorInstanceId)
            .HasConversion(id => id.Value, guid => new CardInstanceId(guid))
            .IsRequired();
        builder.Property(p => p.RecipientInstanceId)
            .HasConversion(id => id.Value, guid => new CardInstanceId(guid))
            .IsRequired();
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Ignore(p => p.DomainEvents);
    }
}
