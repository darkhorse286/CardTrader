using CardTrader.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CardTrader.Infrastructure.Persistence.Configurations;

internal sealed class PackClaimConfiguration : IEntityTypeConfiguration<PackClaim>
{
    public void Configure(EntityTypeBuilder<PackClaim> builder)
    {
        builder.ToTable("pack_claims");
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId)
            .HasConversion(id => id.Value, guid => new UserId(guid));
        builder.Property(p => p.LastClaimedAt).IsRequired();
    }
}
