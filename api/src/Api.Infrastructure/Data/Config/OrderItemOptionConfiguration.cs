using Api.Core.Aggregates.OrderAggregate;

namespace Api.Infrastructure.Data.Config;

public class OrderItemOptionConfiguration : IEntityTypeConfiguration<OrderItemOption>
{
  public void Configure(EntityTypeBuilder<OrderItemOption> builder)
  {
    builder.ToTable("OrderItemOptions");

    builder.HasKey(o => o.Id);

    builder.Property(o => o.GroupName)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(o => o.Label)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(o => o.PriceAdjustment)
      .HasPrecision(18, 2);

    builder.HasIndex(o => o.OrderItemId);
  }
}
