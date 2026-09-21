using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DDD.Domain.Entities;

namespace DDD.Infrastructure.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);

        // Создаем индекс для быстрой фильтрации по энаму
        builder.HasIndex(x => x.Status)
               .HasDatabaseName("IX_Orders_Status");
    }
}
