using DDD.Infrastructure.Log;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DDD.Infrastructure.Configurations;

public sealed class EventLogConfiguration
    : IEntityTypeConfiguration<EventLog>
{
    public void Configure(
        EntityTypeBuilder<EventLog> builder)
    {
        builder.ToTable("EventLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventId)
            .IsRequired();

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.HasIndex(x => x.EventId);
    }
}