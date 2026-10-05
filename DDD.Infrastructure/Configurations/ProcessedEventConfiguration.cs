using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DDD.Infrastructure.Events;

namespace DDD.Infrastructure.Configurations;

public sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("ProcessedEvents");

        builder.HasKey(x => x.EventId);

        builder.Property(x => x.EventId)
            .IsRequired();

        builder.Property(x => x.AttemptCount)
            .IsRequired();

        builder.Property(x => x.ClaimToken);

        builder.Property(x => x.ClaimedUntilUtc);

        builder.Property(x => x.ProcessedOnUtc);

        builder.Property(x => x.FailedOnUtc);

        builder.Property(x => x.LastError)
            .HasMaxLength(4000);

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedOnUtc)
            .IsRequired();

        builder.HasIndex(x => x.ClaimToken);

        builder.HasIndex(x => new
        {
            x.ProcessedOnUtc,
            x.ClaimedUntilUtc
        });
    }
}