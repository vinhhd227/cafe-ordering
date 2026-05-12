using Api.Core.Aggregates.OrderAggregate;

namespace Api.Infrastructure.Data.Config;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
  public void Configure(EntityTypeBuilder<OrderItem> builder)
  {
    builder.ToTable("OrderItems");

    builder.Property(i => i.Note)
      .HasMaxLength(200);

    builder.Property(i => i.UnitPrice)
      .HasPrecision(18, 2);

    builder.Property(i => i.Discount)
      .HasPrecision(18, 2);

    builder.Property(i => i.OptionAdjustment)
      .HasPrecision(18, 2);

    builder.Property(i => i.OptionValueTotal)
      .HasPrecision(18, 2)
      .HasDefaultValue(0);

    builder.HasMany(i => i.SelectedOptions)
      .WithOne()
      .HasForeignKey(o => o.OrderItemId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(i => i.SelectedOptionValues)
      .WithOne()
      .HasForeignKey(o => o.OrderItemId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
