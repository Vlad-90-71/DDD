using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DDD.Domain.Entities;
using DDD.Infrastructure.Converters;

namespace DDD.Infrastructure.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);

        builder.ComplexProperty(x => x.CustomerName, builder =>
        {
            builder.Property(x => x.Value)
                .HasColumnName("CustomerName")
                .HasMaxLength(200)
                .IsRequired();
        });

        builder.Property(x => x.Email)
            .HasConversion<EmailConverter>()
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("IX_Orders_Email_Unique");

        builder.ComplexProperty(x => x.Price, price =>
        {
            price.Property(x => x.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            price.Property(x => x.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Создаем индекс для быстрой фильтрации по энаму
        builder.HasIndex(x => x.Status)
               .HasDatabaseName("IX_Orders_Status");
    }
}
