using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DDD.Infrastructure.Outbox;

namespace DDD.Infrastructure.Configurations;

public class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.OccurredOnUtc)
            .IsRequired();

        builder.Property(x => x.ProcessedOnUtc);

        builder.Property(x => x.ClaimToken);

        builder.Property(x => x.ClaimedUntilUtc);

        builder.HasIndex(x => new
        {
            x.ProcessedOnUtc,
            x.ClaimedUntilUtc
        });
        
        builder.HasIndex(x => x.EventId)
            .IsUnique();
    }
}