using Api.Core.Aggregates.OrderAggregate;

namespace Api.Infrastructure.Data.Config;

public class OrderItemSelectedOptionConfiguration : IEntityTypeConfiguration<OrderItemSelectedOption>
{
  public void Configure(EntityTypeBuilder<OrderItemSelectedOption> builder)
  {
    builder.ToTable("OrderItemSelectedOptions");

    builder.HasKey(o => o.Id);

    builder.Property(o => o.GroupName)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(o => o.ValueName)
      .HasMaxLength(100)
      .IsRequired();

    builder.Property(o => o.UnitPrice)
      .HasPrecision(18, 2);

    builder.HasIndex(o => o.OrderItemId);
  }
}
