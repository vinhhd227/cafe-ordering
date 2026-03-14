using Api.Core.Aggregates.OrderAggregate;

namespace Api.Infrastructure.Data.Config;

public class OrderPromotionConfiguration : IEntityTypeConfiguration<OrderPromotion>
{
  public void Configure(EntityTypeBuilder<OrderPromotion> builder)
  {
    builder.ToTable("OrderPromotions");

    builder.HasKey(op => op.Id);

    builder.Property(op => op.OrderId).IsRequired();
    builder.Property(op => op.PromotionId).IsRequired();

    builder.Property(op => op.PromoCode)
      .HasMaxLength(200)
      .IsRequired();

    builder.Property(op => op.DiscountAmount)
      .HasPrecision(18, 2)
      .IsRequired();

    builder.HasIndex(op => op.OrderId)
      .HasDatabaseName("IX_OrderPromotions_OrderId");

    builder.HasIndex(op => op.PromotionId)
      .HasDatabaseName("IX_OrderPromotions_PromotionId");
  }
}
