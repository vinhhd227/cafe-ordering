using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.OrderAggregate;

namespace Api.Infrastructure.Data.Config;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
  public void Configure(EntityTypeBuilder<OrderItem> builder)
  {
    builder.ToTable("OrderItems");

    builder.Property(i => i.Temperature)
      .HasMaxLength(10)
      .HasConversion(
        v => v == null ? null : v.Name.ToUpperInvariant(),
        v => v == null ? null : DrinkTemperature.FromName(v, true));

    builder.Property(i => i.IceLevel)
      .HasMaxLength(10)
      .HasConversion(
        v => v == null ? null : v.Name.ToUpperInvariant(),
        v => v == null ? null : IceLevel.FromName(v, true));

    builder.Property(i => i.SugarLevel)
      .HasMaxLength(10)
      .HasConversion(
        v => v == null ? null : v.Name.ToUpperInvariant(),
        v => v == null ? null : SugarLevel.FromName(v, true));
  }
}
